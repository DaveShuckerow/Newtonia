/*!
** Copyright (c) 2011 
**
** Jon Martin of www.jon-martin.com & www.fusedworks.com
** MIT Copyright also to Bartek Drodz of www.everyday3d.com for initial outline scripts
**
** If you find this code useful either a) donate, b) get your boss to donate, c) send improvements
**
** Reselling this source code is not permitted and these notices must remain in any distribution.
** However permission to use any of this code in any commercial product is granted.
** Please do acknowledge authorship or individual authorship where appropriate.
** See individual components for seperate MIT Licences.
** If you wish to contact me you can use the following methods:
** email: info@jon-martin.com
**
** The MIT license:
**
** Permission is hereby granted, free of charge, to any person obtaining a copy
** of this software and associated documentation files (the "Software"), to deal
** in the Software without restriction, including without limitation the rights
** to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
** copies of the Software, and to permit persons to whom the Software is furnished
** to do so, subject to the following conditions:
**
** The above copyright notice and this permission notice shall be included in all
** copies or substantial portions of the Software.

** THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
** IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
** FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
** AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
** WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
** CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GeometryBuffer {

	public List<ObjectData> objects;
	public List<Vector3> vertices;
	public List<Vector2> uvs;
	public List<Vector3> normals;
	private bool reorderfaces=false;
	private ObjectData current;
	public class ObjectData {
		public string name;
		public List<GroupData> groups;
		public List<FaceIndices> allFaces;
		public ObjectData() {
			groups = new List<GroupData>();
			allFaces = new List<FaceIndices>();
		}
	}
	
	private GroupData curgr;
	public class GroupData {
		public string name;
		public string materialName;
		public List<FaceIndices> faces;
		public GroupData() {
			faces = new List<FaceIndices>();
		}
		public bool isEmpty { get { return faces.Count == 0; } }
	}
	
	public GeometryBuffer() {
		objects = new List<ObjectData>();
		ObjectData d = new ObjectData();
		d.name = "_spc_default";
		objects.Add(d);
		current = d;
		
		GroupData g = new GroupData();
		g.name = "_spc_default";
		g.materialName = "_spc_default";     
		d.groups.Add(g);
		curgr = g;
		
		vertices = new List<Vector3>();
		uvs = new List<Vector2>();
		normals = new List<Vector3>();
	}
	
	public void PushObject(string name) {
		if(curgr.isEmpty) objects.Remove(current);
		ObjectData n = new ObjectData();
		n.name = name;
		objects.Add(n);
		
		GroupData g = new GroupData();
		g.name = "_spc_default";
		n.groups.Add(g);
		
		curgr = g;
		current = n;
	}
	
	public void PushGroup(string name) {
        if(curgr.isEmpty) current.groups.Remove(curgr);
		GroupData g = new GroupData();
        g.name = name;
        g.materialName = "_spc_default";     
        current.groups.Add(g);
        curgr = g;
     }
        
	public void PushMaterialGroup(string name) { //dae
        //curgr.materialName = name;
		bool found=false;
		foreach(GroupData gd in current.groups) {
			if (gd.name==name) {
				curgr = gd;
				curgr.materialName=name;
				found=true;
				reorderfaces=true;
			} 
		}
		if (!found) {
			if(curgr.isEmpty) {
				current.groups.Remove(curgr);
			} 
			GroupData g = new GroupData();
			g.name = name;
			g.materialName = name;     
			current.groups.Add(g);
			curgr = g;
		}
	}
	
	public string[] ReturnMaterialNames() {
		string[] EmptyMaterials;
		int i=0;
		foreach(ObjectData od in objects) {
			i+=od.groups.Count;
		}
		EmptyMaterials = new string[i];
		i=0;
		foreach(ObjectData od in objects) {
			foreach(GroupData gd in od.groups) {
				EmptyMaterials[i]=gd.materialName;
				i++;
			}
		}		
		return EmptyMaterials;
	}
	
	public void PushVertex(Vector3 v) {
		vertices.Add(v);
	}
	
	public void PushUV(Vector2 v) {
		uvs.Add(v);
	}
	
	public void PushNormal(Vector3 v) {
		normals.Add(v);
	}
	
	public void PushFace(FaceIndices f) {
		curgr.faces.Add(f);
		current.allFaces.Add(f);
	}
	
	//In Progress, Vertex Limit Submesh to Objects Splitter possible.
	//care would need to be taken on relocation/scale of objects at end of build functions
 	public bool ResolveLimit() {
		List<ObjectData> newobjects=new List<ObjectData>();
		bool resolvable=true;
		bool resolved=false;
		foreach(ObjectData od in objects) {
			if (od.allFaces.Count>65000) {
				foreach(GroupData gd in od.groups) {
					if (gd.faces.Count>65000) {
						resolvable=false;
						Debug.Log("Group with more than 65000 Verts");
					}
				} 
				if (resolvable) { 
					Debug.Log("Trying to Resolve");
					foreach(GroupData gd in od.groups) {
						ObjectData n = new ObjectData();
						n.name = gd.name;
						n.groups.Add(gd);
						n.allFaces=gd.faces;
						newobjects.Add(n);
					}
				} else {
					break;
				}
			} else {
				newobjects.Add(od);
			}
		}
		if (resolvable) {
			objects=newobjects;
			resolved=true;
		} else {
			resolved=false;
		}
		return resolved;
	} 	

	
	public bool Check(bool AutoResolveVLimit,bool DebugOut) {
		bool countok=true;
		if (DebugOut) {
			Debug.Log("OBJ has " + objects.Count + " object(s)");
			Debug.Log("OBJ has " + vertices.Count + " vertice(s)");
			Debug.Log("OBJ has " + uvs.Count + " uv(s)");
			Debug.Log("OBJ has " + normals.Count + " normal(s)");
		}
		foreach(ObjectData od in objects) {
			if (DebugOut) {
				Debug.Log(od.name + " has " + od.groups.Count + " group(s)" + od.allFaces.Count + "faces");
			}
			if (od.allFaces.Count>=64000) {
				countok=false;
			}
			foreach(GroupData gd in od.groups) {
				if (DebugOut) {
					Debug.Log(od.name + "/" + gd.name + " has " + gd.faces.Count + " faces(s)");
				}
			}
		}
		/*if (!countok) {  //Not fully implemented
			if (AutoResolveVLimit) {
				countok=ResolveLimit();
			}
		}*/
		return countok;
	}
	
	public int numObjects { get { return objects.Count; } }	
	public bool isEmpty { get { return vertices.Count == 0; } }
	public bool hasUVs { get { return uvs.Count > 0; } }
	public bool hasNormals { get { return normals.Count > 0; } }
	
	public void PopulateMeshes(GameObject[] gs, Dictionary<string, Material> mats) {
		
		if(gs.Length != numObjects) {
			return; // Should not happen unless obj file is corrupt...
		}
		for(int i = 0; i < gs.Length; i++) {
			ObjectData od = objects[i];
			//Order/Sort faces from Groups 
			if (reorderfaces) {
				od.allFaces=new List<FaceIndices>();
				foreach(GroupData gd in od.groups) {
					foreach(FaceIndices f in gd.faces) {
						od.allFaces.Add(f);
					}
				}
			}
			//
			if(od.name != "_spc_default" && gs.Length!=1) gs[i].name = od.name;
			if(od.name != "_spc_default" && gs.Length==1 && gs[0].name=="_spc_rename_") gs[i].name = od.name;
			Vector3[] tvertices = new Vector3[od.allFaces.Count];
			Vector2[] tuvs = new Vector2[od.allFaces.Count];
			Vector3[] tnormals = new Vector3[od.allFaces.Count];
			int k = 0;
			foreach(FaceIndices fi in od.allFaces) {
				tvertices[k] = vertices[fi.vi];
				if(hasUVs) tuvs[k] = uvs[fi.vu];
				if(hasNormals) tnormals[k] = normals[fi.vn];
				k++;
			}
			Mesh m = (gs[i].GetComponent(typeof(MeshFilter)) as MeshFilter).mesh;
			m.vertices = tvertices;
			if(hasUVs) m.uv = tuvs;
			if(hasNormals) m.normals = tnormals;
			bool bumpmap=false;
			if(od.groups.Count == 1) {
				GroupData gd = od.groups[0];
				int[] triangles = new int[gd.faces.Count];
				for(int j = 0; j < triangles.Length; j++) triangles[j] = j;
				m.triangles = triangles;
				gs[i].renderer.material = mats[gd.materialName];
				gs[i].renderer.material.name = gd.materialName;
				if (mats[gd.materialName].shader.name.Contains("Bumped ")) {
					bumpmap=true;
				}
			} else {
				int gl = od.groups.Count;
				Material[] sml = new Material[gl];
				m.subMeshCount = gl;
				int c = 0;
				for(int j = 0; j < gl; j++) {
					sml[j] = mats[od.groups[j].materialName]; 
					sml[j].name = od.groups[j].materialName;
					int[] triangles = new int[od.groups[j].faces.Count]; 

					int l = od.groups[j].faces.Count+c;
					int s = 0;
					for(; c < l; c++, s++) triangles[s] = c;
					m.SetTriangles(triangles, j);
					if (sml[j].shader.name.Contains("Bumped ")) {
						 bumpmap=true;
					}
				}
				gs[i].renderer.materials = sml;
			}
					
			if(!hasNormals) {
				m.RecalculateNormals(); //if vertice count high this could blow!
			}
			if(bumpmap) {
				TangentSolver(m);
			}
		}
	}

	public void TangentSolver(Mesh mesh) {		
		Vector3[] tan2 = new Vector3[mesh.vertices.Length];
		Vector3[] tan1= new Vector3[mesh.vertices.Length];
		Vector4[] tangents = new Vector4[mesh.vertices.Length];
		//Vector3[] binormal = new Vector3[mesh.vertices.Length]; //Remember to release below as well
		
		for (int a = 0; a < (mesh.triangles.Length); a += 3)	{
			long i1 = mesh.triangles[a + 0];
			long i2 = mesh.triangles[a + 1];
			long i3 = mesh.triangles[a + 2];
			
			Vector3 v1 = mesh.vertices[i1];
			Vector3 v2 = mesh.vertices[i2];
			Vector3 v3 = mesh.vertices[i3];
			
			Vector2 w1 = mesh.uv[i1];
			Vector2 w2 = mesh.uv[i2];
			Vector2 w3 = mesh.uv[i3];
			
			float x1 = v2.x - v1.x;
			float x2 = v3.x - v1.x;
			float y1 = v2.y - v1.y;
			float y2 = v3.y - v1.y;
			float z1 = v2.z - v1.z;
			float z2 = v3.z - v1.z;
			
			float s1 = w2.x - w1.x;
			float s2 = w3.x - w1.x;
			float t1 = w2.y - w1.y;
			float t2 = w3.y - w1.y;
			
			float r = 1.0F / (s1 * t2 - s2 * t1);
			Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r,	(t2 * z1 - t1 * z2) * r);
			Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r,	(s1 * z2 - s2 * z1) * r);
			
			tan1[i1] += sdir;
			tan1[i2] += sdir;
			tan1[i3] += sdir;

			tan2[i1] += tdir;
			tan2[i2] += tdir;
			tan2[i3] += tdir;
		}

		for (int a = 0; a < mesh.vertices.Length; a++) {
			Vector3 n =  mesh.normals[a];
			Vector3 t = tan1[a];

			
			Vector3.OrthoNormalize( ref n, ref t );            
			tangents[a].x  = t.x;            
			tangents[a].y  = t.y;           
			tangents[a].z  = t.z;
			
			// Calculate handedness
			tangents[a].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
			
			//To calculate binormals if required as vector3 try one of below:-
			//Vector3 binormal[a] = (Vector3.Cross(n, t) * tangents[a].w).normalized; 
			//Vector3 binormal[a] = Vector3.Normalize(Vector3.Cross(n, t) * tangents[a].w)		
		}
		mesh.tangents = tangents;
	}
}



























