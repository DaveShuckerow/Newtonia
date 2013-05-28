/*!
** Copyright (c) 2011 
**
** Jon Martin of www.jon-martin.com & www.fusedworks.com.
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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml;
using System.Text;

public class DAE : MonoBehaviour {
	
	public string objPath;
	public float objmaxsize=4.0F; //never negative
	public float objminsize=0.3F; //never negative
	public bool EnforceSingleObj=false;
	//public bool AutoResolveVLimit=true; //not fully implemented
	
	private bool initval=false;
	private float MasterScale=1.0F;
	private float minvpos;
	private float maxvpos;
	private float minvxpos;
	private float maxvxpos;
	private float minvypos;
	private float maxvypos;
	private float minvzpos;
	private float maxvzpos;
	private float MasterOffsetx=0;
	private float MasterOffsety=0;
	private float MasterOffsetz=0;
	private Vector3 MainOffset=new Vector3();

	private string basepath;

	private GeometryBuffer buffer;
	private Texture2D[] TMPTextures;
	
	private XmlDocument xdoc;
	
	private int Voffset=0;
	private int Noffset=0;
	private int Uoffset=0;
	private int instanceVoffset=0;
	private int instanceNoffset=0;
	private int instanceUoffset=0;
	
	private int up_axis=1; //always presumed incoming is right handed, Order 0=Xup, 1=Yup, 2=Zup;
	private bool normalvertexgrouped=false;
	private bool treatasoneobject=false;
	private bool firstobject=true;
	private bool hasmaterials=false;
	private string currentgname="";
	private int stwithoutgeometry=0;

	void Start() {
		buffer = new GeometryBuffer ();
		if (objPath!="") {
			if (!objPath.Contains("http://")) {
				basepath=Path.GetDirectoryName(objPath);
				objPath="file://"+objPath;
				basepath="file://"+basepath+Path.DirectorySeparatorChar;
			} else {
				basepath = (objPath.IndexOf("/") == -1) ? "" : objPath.Substring(0, objPath.LastIndexOf("/") + 1);
			}
			StartCoroutine (Load (objPath));
		}
	}
	
	///Main Loop
	public IEnumerator Load(string path) {
		WWW loader = new WWW(path);
		yield return loader;
		if (loader.error != null) {
			Debug.Log("File Not Found");   //!!!!!
		} else {
			ReadDAE(loader.text);
		}
		if (buffer.Check(true,false)) { //buffer.Trace(AutoResolveVLimit)
			if(hasmaterials) {	
				TMPTextures=new Texture2D[TextureList.Count];
				foreach (KeyValuePair<string, int>txt in TextureList) {
					string imgext=Path.GetExtension(txt.Key);
					if (imgext==".jpg"||imgext==".jpeg"||imgext==".png") {
						WWW texloader = new WWW(ResolvePath(txt.Key,basepath));
						yield return texloader;
						if (texloader.error != null) {																					
							Debug.Log("failed to resolve texture path"+basepath+txt.Key);
						} else {
							TMPTextures[txt.Value]=texloader.texture;			
						}
					} else {
						Debug.Log(txt.Key+": image not correct file type please use jpeg or png");  //!!!!
					}
				}
				SolveMaterials();
			} else {
				string[] emats=buffer.ReturnMaterialNames(); 
				materialData = new List<MaterialData>();
				MaterialData nmd = new MaterialData();
				foreach(string mname in emats) {
					nmd = new MaterialData();
					nmd.ID = mname;
					nmd.name = mname;
					nmd.ShaderName="Diffuse";
					nmd.diffuse = new Color(0.5f, 0.5f, 0.5f, 1.0f);
					materialData.Add(nmd);
				}
				hasmaterials=true;
			}
			CheckScale();
			Build();
		} else {
			Debug.Log("Too many poly's");
		}
		Destroy(this);
	}	
	
	private void GetUpAxis() { //All assumed right handed - collada
		if ((XmlElement) xdoc.GetElementsByTagName("up_axis")[0]!=null) {
			XmlElement ObjAxis = (XmlElement) xdoc.GetElementsByTagName("up_axis")[0];
			switch (ObjAxis.InnerXml) {
				case "X_UP": 
					up_axis=0;
				break;
				case "Y_UP": 
					up_axis=1;
				break;
				case "Z_UP":
					up_axis=2;
				break;
			}
		} else {
			up_axis=1;
		}
	}
	
	private void ReadDAE(string gdata) {
		buffer = new GeometryBuffer ();		
		xdoc=new XmlDocument();
		xdoc.LoadXml(gdata); 
		GetUpAxis();
		GetDAEMaterials();
		if (GetSceneGeo()) {
			foreach(XmlNode geometry in xdoc.GetElementsByTagName("geometry")) {   
				///currentgeoname="";
				currentgname="";
				Voffset=buffer.vertices.Count>0?(buffer.vertices.Count):0;
				Uoffset=buffer.uvs.Count>0?(buffer.uvs.Count):0;
				Noffset=buffer.normals.Count>0?(buffer.normals.Count):0;
				XmlElement geonode = geometry as XmlElement;
				string gtype=CanParseGeometry(geonode);
				if (gtype!="false") {
					currentgname=geonode.Attributes["id"].Value;
					bool inscene=false;
					foreach(SceneGeo g in sceneGeo) {
						if (currentgname==g.GID) {
							inscene=true;
						}
					}
					if (inscene) {
						if (GetGeometryData(geonode)) {
								if (GetFaceData(geonode,gtype)) {
								} else {
									Debug.Log ("Geometry "+geonode.Attributes["id"].Value+"has no readable face data");
								}
						} else {
							Debug.Log ("Geometry "+geonode.Attributes["id"].Value+" has no readable source data");
						}
					} else {
						// Debug.Log (currentgname+"notinscene");
					}
				} else {
					// Debug.Log ("Cannot parse this geometry");
				}
			} 
		} else {
			Debug.Log ("Cannot get scene data matching geometry to materials");
		}
	}

	/////////////////////////////////////////////SCENE ID SORTING
	private Vector3 UpPosConv(Vector3 v) {
		Vector3 r=new Vector3();
		//All x,y,z Up right handed to convert to Y_up left handed Position
		switch (up_axis) {
			case 0: //xR-yL
				r=new Vector3(0-v.y,v.x,0-v.z);
				break;
			case 1: //yR-yL
				r=new Vector3(v.x,v.y,0-v.z);
				break;
			case 2:	//zR-zL
				r=new Vector3(v.x,v.z,v.y);
				break;
		}
		return r;
	}		
	
	private Vector3 UpScaleConv(Vector3 v) {
		Vector3 r=new Vector3();
		//All x,y,z Up right handed to convert to Y_up left handed Position
		switch (up_axis) {
			case 0: //xR-yL
				r=new Vector3(v.y,v.x,v.z);
				break;
			case 1: //yR-yL
				r=new Vector3(v.x,v.y,v.z);
				break;
			case 2:	//zR-zL
				r=new Vector3(v.x,v.z,v.y);
				break;
		}
		return r;
	}

	private Vector3 UpRotDirConv(Vector3 v) {
		Vector3 r=new Vector3();
		//All x,y,z Up right handed to convert to Y_up left Rotation //Currently reversed rotation on all axis
		switch (up_axis) {
			case 0: //xR-yL
				r=new Vector3(v.y,0-v.x,v.z);
				break;
			case 1: //yR-yL
				r=new Vector3(0-v.x,0-v.y,v.z);
				break;
			case 2:	//zR-zL
				r=new Vector3(0-v.x,0-v.z,0-v.y);
				break;
		}
		return r;
	}	
	
	///Geometry GETSCENEGEometry
	///Lookup list to provide Geometry matches to MaterialDataList also provide any per Geometry Transforms
	private List<SceneGeo> sceneGeo = new List<SceneGeo>();
	private class SceneGeo {
		public string ParentNode;
		public string GID;
		public bool isInstance=false;
		public Matrix4x4 Gm;
		public Quaternion Totalrot;
		public List<GMaterialID> GMaterial;
		public SceneGeo() {
			GMaterial = new List<GMaterialID>();
		}
	}
	
	private class GMaterialID{
		public string id;
		public string symbol;
	}
	
	private List<SceneTransform> sceneTransforms = new List<SceneTransform>();
	private class SceneTransform{
		public string NodeName;
		public Vector3 pos;
		public Quaternion rot;
		public Vector3 scl;
		public bool hasgeometry;
		public string ParentNode;
	}
	
	private bool GetSceneGeo() {
		bool success=false;
		if ((XmlElement) xdoc.GetElementsByTagName("library_visual_scenes")[0]!=null) {
			success=true;
		}
		if (success) {
			XmlElement sdata=(XmlElement) xdoc.GetElementsByTagName("library_visual_scenes")[0];
			XmlElement scdata=(XmlElement) sdata.GetElementsByTagName("visual_scene")[0];
			XmlNode child = scdata.FirstChild;
			child = scdata.FirstChild;
			int level=0;
			int blanknameapnd=0;
			List <Vector3>  Tpos = new List<Vector3>();
			List <Vector3>  Tscl = new List<Vector3>();
			List <Quaternion>  Trot = new List<Quaternion>();
			List <XmlNode> traverseNodes = new List<XmlNode>();
			traverseNodes.Add(child);
            while(level!=-1 && child is XmlElement) { //Traverse Scene Nodes
				XmlElement childnode = child as XmlElement;
                if (child.Name=="node") {
					SceneTransform st = new SceneTransform();
					if (childnode.HasAttribute("id")) {
						st.NodeName=childnode.Attributes["id"].Value;
					} else {
						if (childnode.HasAttribute("name")) {
							st.NodeName=childnode.Attributes["name"].Value;
						} else {
							st.NodeName="_spc_offset_"+blanknameapnd;
							childnode.SetAttribute("id",st.NodeName);
							++blanknameapnd;
						}
					}

					Vector3 tp=new Vector3(0,0,0);
					Vector3 ts=new Vector3(1,1,1);
					Quaternion tr=Quaternion.identity;
					if (childnode["translate"]!=null||childnode["scale"]!=null||childnode["rotate"]!=null) {
						if (childnode["translate"]!=null) {
							string data=RemoveWS(childnode["translate"].InnerXml);
							string[] vdata = data.Split(" ".ToCharArray());  
							tp=new Vector3(cf(vdata[0]),cf(vdata[1]),cf(vdata[2])); //check positioning!!!!>>???!?!?!?!?
							tp=UpPosConv(tp); //Sort/reverse co-ords
						}
						if (childnode["scale"]!=null) {
							string data=RemoveWS(childnode["scale"].InnerXml);
							string[] vdata = data.Split(" ".ToCharArray());  
							ts=new Vector3(cf(vdata[0]),cf(vdata[1]),cf(vdata[2])); //check scaling!!!!>>???!?!?!?!?
							ts=UpScaleConv(ts); //sort co-ords
						}
						if (childnode["rotate"]!=null) {
							XmlNode underchild = childnode.FirstChild;
							while(underchild != null && underchild is XmlElement) {
								if (underchild.Name=="rotate") {
									string data=RemoveWS(underchild.InnerXml);
									string[] vdata = data.Split(" ".ToCharArray());  
									Vector3 tdir=new Vector3(cf(vdata[0]),cf(vdata[1]),cf(vdata[2]));
									tdir=UpRotDirConv(tdir); //Sort&reverse direction
									tr *=Quaternion.AngleAxis(cf(vdata[3]),tdir);
								}
								underchild = underchild.NextSibling;                
							}
						}
					} 
					st.pos=tp;
					st.scl=ts;
					st.rot=tr;
					Tpos.Add(tp);
					Tscl.Add(ts);
					Trot.Add(tr);
					//purely for import of single objects or if multiple working out the realscale of vertex used, so we can resize the scene/group correctly
					if (child.ParentNode!=null) {
						XmlElement pN = child.ParentNode as XmlElement;
						if (pN.Name=="node") {
							if (pN.HasAttribute("id")) {
								st.ParentNode=pN.Attributes["id"].Value;
							} else {
								if (pN.HasAttribute("name")) {
									st.ParentNode=pN.Attributes["name"].Value;
								}
							}
						} 
					}
					if (childnode["instance_geometry"]!=null) {
						st.hasgeometry=true;
						XmlNode underchild = childnode.FirstChild;
						while( underchild != null && underchild is XmlElement) {
							if (underchild.Name=="instance_geometry") {
								SceneGeo sg = new SceneGeo();
								sg.GID = underchild.Attributes["url"].Value.Replace("#","");
								SceneGeo chkinst = sceneGeo.Find(delegate(SceneGeo item) { return item.GID == sg.GID; }); 
								if (chkinst!=null) {
									sg.isInstance=true;
								}
								sg.ParentNode = st.NodeName;
								XmlElement underchildE = underchild as XmlElement;
								foreach (XmlNode instancemat in underchildE.GetElementsByTagName("instance_material")) {
									XmlElement imat = instancemat as XmlElement;
									GMaterialID gm = new GMaterialID();	
									gm.id=imat.Attributes["target"].Value.Replace("#","");
									gm.symbol=imat.Attributes["symbol"].Value;
									sg.GMaterial.Add(gm);
								}
								Vector3 cp=new Vector3(0,0,0);
								Vector3 cs=new Vector3(1,1,1);
								Quaternion cr=Quaternion.identity;
								Matrix4x4 NodeMatrix=Matrix4x4.TRS(cp,cr,cs);
								for (int i=0;i<Tpos.Count;i++) { //work out end transform for the vertex's belonging to this geometry
									cp=NodeMatrix.MultiplyPoint3x4(Tpos[i]);
									cs=Vector3.Scale(cs,Tscl[i]);
									cr*=Trot[i];
									NodeMatrix=Matrix4x4.TRS(cp,cr,cs);
								}
								Matrix4x4 m=NodeMatrix; 
								sg.Totalrot=cr;
								sg.Gm=m;
								sceneGeo.Add(sg);
							}
							underchild = underchild.NextSibling;
						}
					} else {
						st.hasgeometry=false;
					}
					if (childnode["node"]!=null||st.hasgeometry) {
						sceneTransforms.Add(st);
					}
				}	
				if (childnode["node"]!=null) { 
					child = childnode["node"] as XmlElement;
					traverseNodes.Add(child);
					++level;
				} else {
					bool resolve=false;
					while(!resolve) {
						child = child.NextSibling; //next node
						if (child==null) { //backup a level
							traverseNodes.RemoveAt(traverseNodes.Count-1);
							if (Tpos.Count!=0) {
								Tpos.RemoveAt(Tpos.Count-1);
								Tscl.RemoveAt(Tscl.Count-1);
								Trot.RemoveAt(Trot.Count-1);
							}
							if (traverseNodes.Count!=0) {
								child = traverseNodes[traverseNodes.Count-1];
							}
							--level;
							if (level==-1) {
								resolve=true;
							}
						} else {
							traverseNodes.RemoveAt(traverseNodes.Count-1);
							if (Tpos.Count!=0) {
								Tpos.RemoveAt(Tpos.Count-1);
								Tscl.RemoveAt(Tscl.Count-1);
								Trot.RemoveAt(Trot.Count-1);
							}
							traverseNodes.Add(child); //next on same level
							resolve=true;
						}
					}
				}
            }  
			
		}
		treatasoneobject=false;
		int k=0;
		stwithoutgeometry=0;
		foreach(SceneTransform st in sceneTransforms) {
			if (!st.NodeName.Contains("_spc_offset_")) {
				++k;
			}
			if (!st.hasgeometry) {
				++stwithoutgeometry;
			}
		} 
		
		if (k==1) {
			treatasoneobject=true;
		}
		if (EnforceSingleObj) {
			treatasoneobject=true;
		}
		// could also check faces if more than 65000 and materiallist>0 treat as different objects here, or exit strategy!
		return success;
	}

	/////////////////////////////////////////////GEOMETRY SORTING
	private string CanParseGeometry(XmlElement georoot) {
		//returns "triangles", "polylist", "polygons" or "false" if not mesh, is lines, or type unknown for current geometry.
		string val="false";
		if ((XmlElement) georoot.GetElementsByTagName("mesh")[0]!=null) {
			if ((XmlElement) georoot.GetElementsByTagName("lines")[0] == null) {
				if ((XmlElement) georoot.GetElementsByTagName("triangles")[0]!=null) {
					val="triangles";
				} else {
					if ((XmlElement) georoot.GetElementsByTagName("polygons")[0]!=null) {
						val="polygons";
					} else {
						if ((XmlElement) georoot.GetElementsByTagName("polylist")[0]!=null) {
							val="polylist";
						} else {
							val="false";
						} 
					}
				}
			} else {
				val="false";
			}
		} else {
			val="false";
		}
		return val;
	}
	
	private class DataID {
		public string ID;
		public string Semantic;
	}
	
	private void pushtooffsetscale(Vector3 v) {
		if (!initval) {
			minvxpos=v.x;
			maxvxpos=v.x;
			minvypos=v.y;
			maxvypos=v.y;
			minvzpos=v.z;
			maxvzpos=v.z;
			initval=true;
		} else {
			minvxpos=v.x<minvxpos?v.x:minvxpos;
			maxvxpos=v.x>maxvxpos?v.x:maxvxpos;
			minvypos=v.y<minvypos?v.y:minvypos;
			maxvypos=v.y>maxvypos?v.y:maxvypos;
			minvzpos=v.z<minvzpos?v.z:minvzpos;
			maxvzpos=v.z>maxvzpos?v.z:maxvzpos;
		}
	}
	
	private bool GetGeometryData(XmlElement georoot) {
		//get vertex, normal and uv data from current geometry returns true if something at least was readable.
		bool success=false;
		List<DataID> sourceIds;
		sourceIds = new List<DataID>();
		XmlNodeList datapointers = georoot.GetElementsByTagName("input");
		foreach(XmlNode dpoint in datapointers) {	
			XmlElement dp = dpoint as XmlElement;
			DataID sID = new DataID();
			sID.ID = dp.Attributes["source"].Value.Replace("#","");
			sID.Semantic = dp.Attributes["semantic"].Value;     
				if (sID.Semantic=="NORMAL") { //important, sometimes normals are referenced with the same index as vertex positions;
					if (dp.ParentNode.Name=="vertices") {
						normalvertexgrouped=true; 
					}						
				}
			sourceIds.Add(sID);
		}	

		List<SceneGeo> currentgeosd=new List<SceneGeo>(); // DEAL WITH INSTANCING
		SceneGeo curg=new SceneGeo();
		foreach(SceneGeo g in sceneGeo) {
			if (g.GID==currentgname) {
				SceneGeo sg=new SceneGeo();
				sg=g;
				currentgeosd.Add(sg);
			}
		}
		int TotalInstances=1;
		if (treatasoneobject) {
			TotalInstances=currentgeosd.Count;
		} 
		
		for (int inst=0;inst<TotalInstances;inst++) { //V.BAD parses data again for each instance when working as one object - very_uncommon - optimise?
			curg=currentgeosd[inst];
			foreach(XmlNode SourceData in georoot.GetElementsByTagName("source")) { 	
				XmlElement source = SourceData as XmlElement;
				foreach(DataID sID in sourceIds) {
					if (SourceData.Attributes["id"].Value==sID.ID) {			
						XmlElement DataArray = (XmlElement) source.GetElementsByTagName("float_array")[0];
						XmlElement Technique = (XmlElement) source.GetElementsByTagName("accessor")[0];
						int stride=int.Parse(Technique.Attributes["stride"].Value);
						int elements=int.Parse(Technique.Attributes["count"].Value);
						int total=int.Parse(DataArray.Attributes["count"].Value);
						string data=RemoveWS(DataArray.InnerXml); //try removing of optimising this
						string[] fdata = data.Split(" ".ToCharArray());  //try setting char delimeter before //StringBuilder usage perhaps
						if (fdata.Length==total) {
							Vector3 v;
							Vector3 vi=new Vector3();
							for (int j=0;j<fdata.Length;j+=stride) {		
								if (sID.Semantic=="POSITION") {										// VERTEX DATA
									instanceVoffset=elements;
									v=new Vector3(cf(fdata[j]),cf(fdata[j+1]),cf(fdata[j+2]));
									v=UpPosConv(v);								
									if (treatasoneobject) { //runs and creates geometry for every instance in larger loop
										v=curg.Gm.MultiplyPoint3x4(v);
										buffer.PushVertex(v);
										pushtooffsetscale(v);
									} else {
										buffer.PushVertex(v); //should run larger loop once and create geometry once, but cycle positions for correct scaling on instances
										foreach(SceneGeo sg in currentgeosd) { 
											vi=sg.Gm.MultiplyPoint3x4(v);
											pushtooffsetscale(vi);
										}
									}
									success=true;
								}
								if (sID.Semantic=="NORMAL") {																			//NORMAL DATA
									instanceNoffset=elements;
									v=new Vector3(cf(fdata[j]),cf(fdata[j+1]),cf(fdata[j+2]));
									v=UpPosConv(v);
									if (treatasoneobject) {
										v=curg.Totalrot*v;
									} //else should come out in wash?
									buffer.PushNormal(v); 	
									success=true;
								}
								if (sID.Semantic=="TEXCOORD") {																		//UV DATA
									instanceUoffset=elements;
									buffer.PushUV(new Vector2( cf(fdata[j]), cf(fdata[j+1]) ));
									success=true;
								}
							}
						} else {
							Debug.Log ("In Geometry "+georoot.Attributes["id"].Value +" have problem parsing xml @ "+sID.Semantic);
						}
					}
				}
			}
		}
		return success;
	}
	
	private string[] FormatFaceData(XmlElement FaceSection,int IndiceCount,int FaceCount) {
		//Return Face Data from xml facenode in formated face string[];
		string[] Fdata=new string[FaceCount];
		XmlNodeList DataArray = FaceSection.GetElementsByTagName("p");
		if (DataArray.Count>0) {
			int Fcnt=0;
			if (DataArray.Count<2) {
				XmlElement polylistcheck = (XmlElement) FaceSection.GetElementsByTagName("vcount")[0];
				XmlElement DArray = (XmlElement) FaceSection.GetElementsByTagName("p")[0];
				if (polylistcheck!=null) { //polylist
					string[] PLILength=polylistcheck.InnerXml.Split(" ".ToCharArray());
					string ds=RemoveWS(DArray.InnerXml);
					string[] TMPdata = ds.Split(" "[0]);  
					int i=0;	int l=0;
					while (i<TMPdata.Length) {
						StringBuilder facepart = new StringBuilder();
							for (int j=0;j<int.Parse(PLILength[l]);++j) {
								for (int k=0;k<IndiceCount;++k) {
									facepart.Append(TMPdata[i+(j*IndiceCount)+k]).Append("/"); 
								}
								facepart.Remove(facepart.Length-1, 1);
								facepart.Append(" ");
							}
							Fdata[Fcnt]=facepart.ToString();
							++Fcnt;
							i+=(int.Parse(PLILength[l])*IndiceCount);
							++l;
						}
				} else { //triangles
					string ds=RemoveWS(DArray.InnerXml);
					string[] TMPdata = ds.Split(" "[0]);  
					for (int i=0;i<TMPdata.Length;i+=(3*IndiceCount)){
						StringBuilder facepart = new StringBuilder();
						for (int j=0;j<3;++j) {
							for (int k=0;k<IndiceCount;++k) {
								facepart.Append(TMPdata[i+(j*IndiceCount)+k]).Append("/"); 
							}
							facepart.Remove(facepart.Length-1, 1);
							facepart.Append(" ");	
						}
						Fdata[Fcnt]=facepart.ToString();
						++Fcnt;
					}
				}
			} else { //polys
				foreach(XmlNode poly in DataArray) {
					string ds=RemoveWS(poly.InnerXml);
					string[] TMPdata = ds.Split(" "[0]); 
					StringBuilder facepart = new StringBuilder();
					for (int i=0;i<TMPdata.Length;i+=IndiceCount){
						for (int k=0;k<IndiceCount;k++) {
							facepart.Append(TMPdata[i+k]).Append("/"); 
						}
						facepart.Remove(facepart.Length-1, 1);
						facepart.Append(" ");		
					}
					Fdata[Fcnt]=facepart.ToString();
					++Fcnt;
				}
			}
		} else {
			Debug.Log ("Cannot Find Any Face Data For this Geometry");
		}
		return Fdata;
	}
	
	private bool GetFaceData(XmlElement georoot,string gtype) {
		//Get Faces Indices, vert,uv,norm push to material submesh group
		bool success=false;
		if (firstobject) {
			buffer.PushObject(currentgname);
			if (treatasoneobject) {
				firstobject=false;
			} 
		}

		List<SceneGeo> currentgeosd=new List<SceneGeo>(); // GET INSTANCING
		SceneGeo curg=new SceneGeo();
		foreach(SceneGeo g in sceneGeo) {
			if (g.GID==currentgname) {
				SceneGeo sg=new SceneGeo();
				sg=g;
				currentgeosd.Add(sg);
			}
		}
			
		int TotalInstances=1;
		if (treatasoneobject) {
			TotalInstances=currentgeosd.Count;
		} 
		for (int inst=0;inst<TotalInstances;++inst) { //V.BAD parses face data again for each instance when working as one object - very_uncommon - optimise?
			curg=currentgeosd[inst];		
			int ivof=instanceVoffset*inst;
			int inof=instanceNoffset*inst;
			int iuof=instanceUoffset*inst;
			foreach(XmlNode FaceSection in georoot.GetElementsByTagName(gtype)) { 
				XmlElement FaceSect= FaceSection as XmlElement;
				if (int.Parse(FaceSect.Attributes["count"].Value)!=0) {
					string cmat="_spc_default"; 

					bool found=false;
					foreach(GMaterialID gmat in curg.GMaterial) {
						if (gmat.symbol==FaceSect.Attributes["material"].Value) {
							cmat=gmat.id; //get real material name
							found=true;
						}
					}
					if (found==false) {
						cmat=currentgname;
					}
					buffer.PushMaterialGroup(cmat);
					XmlNodeList FaceInfo = FaceSect.GetElementsByTagName("input");
					string[] IndiceStruct=new string[FaceInfo.Count];
					for (int i=0; i < FaceInfo.Count; i++) { 
						IndiceStruct[i]=FaceInfo[i].Attributes["semantic"].Value;
					}
					string[] Fdata=FormatFaceData(FaceSect,IndiceStruct.Length,int.Parse(FaceSect.Attributes["count"].Value)); //format data
					TriPoly triangulation;
					foreach(string FCE in Fdata) {	
						string face=RemoveWS(FCE);
						string[] p= face.Split(" ".ToCharArray());
						string[] c;
						if (p.Length<=4) {//is triangle or quad
							for (int j=0;j<p.Length-2;j++) {	//get all possible triangles from line
								FaceIndices fi = new FaceIndices();	//1 vert
								c=p[0].Trim().Split("/".ToCharArray());
									for (int i=0;i<IndiceStruct.Length;i++) {
										if (IndiceStruct[i]=="VERTEX")  {
											fi.vi = ci(c[i])+Voffset+ivof;
											if (normalvertexgrouped) {
												fi.vn = ci(c[i])+Noffset+inof;
											}
										}
										if (IndiceStruct[i]=="NORMAL") {
											fi.vn = ci(c[i])+Noffset+inof;
										}
										if (IndiceStruct[i]=="TEXCOORD") {
											fi.vu=ci(c[i])+Uoffset+iuof;
										}
									}
								buffer.PushFace(fi);
								for (int k=0;k<2;k++) {	//2nd and 3rd vert
									fi = new FaceIndices();	
										int no=2-k+j; //		To invert faces replace with : int no=1+k+j;
										c=p[no].Trim().Split("/".ToCharArray());
										for (int i=0;i<IndiceStruct.Length;i++) {
												if (IndiceStruct[i]=="VERTEX") {
													fi.vi = ci(c[i])+Voffset+ivof;
													if (normalvertexgrouped) {
														fi.vn = ci(c[i])+Noffset+inof;
													}
												}
												if (IndiceStruct[i]=="NORMAL") {
													fi.vn = ci(c[i])+Noffset+inof;
												}
												if (IndiceStruct[i]=="TEXCOORD") {
													fi.vu=ci(c[i])+Uoffset+iuof;
												}
										}
									buffer.PushFace(fi);
								}
							}
						} else { //is poly convert to triangles and push
							triangulation = new TriPoly();
							Vector3[] pointlist=new Vector3[p.Length];
							//Vector3[] normallist=new Vector3[p.Length];
							for (int j=0;j<p.Length;j++) { //go through each faceindex in poly list and pull relevant vertice from geometrybuffer add to vector[]
								c=p[j].Trim().Split("/".ToCharArray());
								for (int i=0;i<IndiceStruct.Length;i++) {
									if (IndiceStruct[i]=="VERTEX")  {
										pointlist[j] =  buffer.vertices[ci(c[i])+Voffset+ivof];
										//if (normalvertexgrouped) {
										//	normallist[j]= ci(c[i])+Noffset+inof;
										//}
									}
									//if (IndiceStruct[i]=="NORMAL") {
									//	normallist[j] = ci(c[i])+Noffset+inof;
									//}
								}
							}
							int[] indices;
							//if (normallist!=null) {
							//	indices=triangulation.Patch(pointlist, normallist);
							//} else {
							indices=triangulation.Patch(pointlist); //, normallist
							//}
							if (indices.Length>2) {
								for (int j=0;j<indices.Length;++j) { //may need to reverse this?
									FaceIndices fi = new FaceIndices();	
									c=p[indices[j]].Trim().Split("/".ToCharArray());
									for (int i=0;i<IndiceStruct.Length;i++) {
											if (IndiceStruct[i]=="VERTEX") {
												fi.vi = ci(c[i])+Voffset+ivof;
												if (normalvertexgrouped) {
													fi.vn = ci(c[i])+Noffset+inof;
												}
											}
											if (IndiceStruct[i]=="NORMAL") {
												fi.vn = ci(c[i])+Noffset+inof;
											}
											if (IndiceStruct[i]=="TEXCOORD") {
												fi.vu=ci(c[i])+Uoffset+iuof;
											}
									}		
									buffer.PushFace(fi);
								}
							} 
						}
					}
					success=true;
				} 
			}
		}
		return success;
	}
		
	private void CheckScale() {
		MasterOffsetx=0.0f-((maxvxpos/2.0f)+(minvxpos/2.0f));
		MasterOffsety=0.0f-((maxvypos/2.0f)+(minvypos/2.0f));
		MasterOffsetz=0.0f-((maxvzpos/2.0f)+(minvzpos/2.0f));
		MainOffset=new Vector3(MasterOffsetx,MasterOffsety+((maxvypos-minvypos)/2),MasterOffsetz);
		minvpos=minvypos<minvxpos?minvypos:minvxpos;
		minvpos=minvzpos<minvpos?minvzpos:minvpos;
		maxvpos=maxvypos>maxvxpos?maxvypos:maxvxpos;
		maxvpos=maxvzpos>maxvpos?maxvzpos:maxvpos;
		float ep=0.0F;
		ep=(0-minvpos)>maxvpos?(0-minvpos):maxvpos;
		if (ep!=0.0f) {
			if (ep>objmaxsize) MasterScale=objmaxsize/ep;
			if (ep<objminsize) MasterScale=objminsize/ep;
			if (treatasoneobject) {
				for(int i=0;i<buffer.vertices.Count;i++) {
					buffer.vertices[i]=(buffer.vertices[i]+MainOffset)*MasterScale;
				}
			} else {
				for(int i=0;i<buffer.vertices.Count;i++) {
					buffer.vertices[i]=buffer.vertices[i]*MasterScale;
				}
			}
		}
		
	}

	/////////////////////////////////////////////MATERIAL SORTING
	
	private List<DAETextureID> daeTextures;
	private class DAETextureID {
		public string id;
		public string path;
	}

	Dictionary<string, int> TextureList = new Dictionary<string, int>();  
	private List<MaterialData>  materialData;
	private class MaterialData {
		public string ID;
		public string name;
		public Color ambient;
   		public Color diffuse;
   		public Color specular=new Color(0.0F,0.0F,0.0F,1.0F);
		public Color emmisive;
		public Color alphacolor=new Color(1.0F,1.0F,1.0F,1.0F);
   		public float shininess=2.0F;
   		public float alpha=1.0F;
   		//public int illumType;
		//public string ambientTexPath;
   		public string diffuseTexPath;
		public string emmisiveTexPath;
		public string specularTexPath;
		public string alphaTexPath;
		//public string bumpTexPath; //not implemented
		public Texture2D DiffTexture; 
		//public Texture2D BumpTexture; //not implemented
		public Texture2D EmmisiveTexture;
		public string ShaderName;
	}
				
	private void GetDAEMaterials() {
		hasmaterials=true;
		int texturecount=0;	
		//Get material and texture data
		daeTextures = new List<DAETextureID>();
		foreach(XmlNode imagepaths in xdoc.GetElementsByTagName("image")) {    
			XmlElement imagepath = imagepaths as XmlElement;
			DAETextureID tID = new DAETextureID();
			tID.id =imagepath.Attributes["id"].Value;
			XmlElement pathdata = (XmlElement) imagepath.GetElementsByTagName("init_from")[0];
			tID.path = pathdata.InnerXml;     
			daeTextures.Add(tID);
		}
		materialData = new List<MaterialData>();
		List<String> MatLookup=new List<string> {"emission", "ambient", "specular", "diffuse", "shininess", "transparent", "transparency"}; 
		XmlElement LibMat= (XmlElement) xdoc.GetElementsByTagName("library_materials")[0];
		XmlElement LibFx= (XmlElement) xdoc.GetElementsByTagName("library_effects")[0];
		if ((XmlElement) xdoc.GetElementsByTagName("library_materials")[0]!=null) {
			foreach(XmlNode mat in LibMat.GetElementsByTagName("material")) {
				XmlElement cmat = mat as XmlElement;
				MaterialData current = new MaterialData();
				current.ID=cmat.Attributes["id"].Value;
				if (cmat.HasAttribute("name")) {
					current.name=cmat.Attributes["name"].Value;
				}
				XmlElement effectnodes = (XmlElement) cmat.GetElementsByTagName("instance_effect")[0];
				string effectid = effectnodes.Attributes["url"].Value.Replace("#",""); 
				foreach(XmlNode effectnode in LibFx.GetElementsByTagName("effect")) {
					XmlElement effect = effectnode as XmlElement;
					if (effect.Attributes["id"].Value==effectid) { //get effect data
						foreach(String lookup in MatLookup) {    
							XmlElement element = (XmlElement) effect.GetElementsByTagName(lookup)[0];
							if (element!=null) {
								XmlElement ecolor=(XmlElement) element.GetElementsByTagName("color")[0];
								XmlElement efloat=(XmlElement) element.GetElementsByTagName("float")[0];
								XmlElement etexture=(XmlElement) element.GetElementsByTagName("texture")[0];
								switch (lookup) {
									case "diffuse":
										if (ecolor!=null) { current.diffuse=DAEGetColor(ecolor);}
										if (etexture!=null) { 
											current.diffuseTexPath=GetValidTexturePath(etexture.Attributes["texture"].Value,effect);		
											if (current.diffuseTexPath!=null&&!TextureList.ContainsKey(current.diffuseTexPath)) { 
												TextureList.Add(current.diffuseTexPath, texturecount);
												texturecount++;
											}
										}
										break;
									case "emission":
										if (ecolor!=null) { current.emmisive=DAEGetColor(ecolor);}
										if (etexture!=null) { 
											current.emmisiveTexPath=GetValidTexturePath(etexture.Attributes["texture"].Value,effect);
											if (current.emmisiveTexPath!=null&&!TextureList.ContainsKey(current.emmisiveTexPath)) { 
												TextureList.Add(current.emmisiveTexPath, texturecount);
												texturecount++;
											}
										}
										break;
									case "ambient":
										if (ecolor!=null) { current.ambient=DAEGetColor(ecolor);}
										/*	if (etexture!=null) { //not implemented
												current.ambientTexPath=GetValidTexturePath(etexture.Attributes["texture"].Value,effect);
												if (current.ambientTexPath!=null&&!TextureList.ContainsKey(current.ambientTexPath)) { 
													TextureList.Add(current.ambientTexPath, texturecount);
													texturecount++;
												}
											} 
										*/
										break;
									case "specular":
										if (ecolor!=null) {current.specular=DAEGetColor(ecolor);}
										if (etexture!=null) { 
											current.specularTexPath=GetValidTexturePath(etexture.Attributes["texture"].Value,effect);
											if (current.specularTexPath!=null&&!TextureList.ContainsKey(current.specularTexPath)) { 
												TextureList.Add(current.specularTexPath, texturecount);
												texturecount++;
											}
										}
										break;
									case "shininess":
										if (efloat!=null) {current.shininess=(cf(efloat.InnerXml));}
										break;
									case "transparent":
										if (ecolor!=null) { current.alphacolor=DAEGetColor(ecolor);} 
										if (etexture!=null) { 
											current.alphaTexPath=GetValidTexturePath(etexture.Attributes["texture"].Value,effect);
											if (current.alphaTexPath!=null&&!TextureList.ContainsKey(current.alphaTexPath)) { 
												TextureList.Add(current.alphaTexPath, texturecount);
												texturecount++;
											}
										}
										break;
									case "transparency":
										if (efloat!=null) {current.alpha=cf(efloat.InnerXml);}
										break;
								}
							}
						}
					}
				}
				materialData.Add(current);
			}
		} else {
			hasmaterials=false;
		}
	}
	
	private Color DAEGetColor(XmlElement elem) {
		string colstring=RemoveWS(elem.InnerXml);
		string[] cols= colstring.Split(" ".ToCharArray());
		return new Color( cf(cols[0]), cf(cols[1]), cf(cols[2]), cf(cols[3]));
	}
	
	private String GetValidTexturePath(string ID,XmlElement fx) {		
		string path="";
		foreach(DAETextureID txt in daeTextures) {
			if (txt.id==ID) {
				path=txt.path;
			}
		}
		if (path=="") {
			bool found=false;
			XmlNodeList fxnodes=fx.GetElementsByTagName("newparam");
			for(int i=0;i<fxnodes.Count;i++) {  //Loop enough times to find without forcing an indefinate loop
				foreach(XmlNode np in fxnodes) {
					XmlElement element = np as XmlElement;
					if (element.HasAttribute("sid")) {
						if (element.Attributes["sid"].Value==ID) {
							if ((XmlElement) element.GetElementsByTagName("source")[0]!=null) {
								XmlElement src=(XmlElement) element.GetElementsByTagName("source")[0];
								ID=src.InnerXml;
							} else {
								if ((XmlElement) element.GetElementsByTagName("init_from")[0]!=null) {
									XmlElement init=(XmlElement) element.GetElementsByTagName("init_from")[0];
									ID=init.InnerXml;
									found=true;
								}
							}
						}
					} else {
						Debug.Log ("Problems accessing texture path: - no SID");
					}
				}
				if (found) {
					break;
				}
			}
			if (found) {
				foreach(DAETextureID txt in daeTextures) {
					if (txt.id==ID) {
						path=txt.path;
					}
				}
			}
		}
		return path;
	}

	private void SolveMaterials() {
		Color[] src;
		Color[] dest;
		Color[] tmp;
			
		foreach(MaterialData m in materialData) { 
			Texture2D AlphaChannel=new Texture2D(2,2);
			//get a diffuse color
			if (m.diffuse.grayscale==0) {		
				if (m.ambient.grayscale!=0) {
					m.diffuse=m.ambient;
				} else {					
					if (m.emmisive.grayscale!=0) {
						m.diffuse=m.emmisive;
					} else {
						m.diffuse=new Color(0.5F,0.5F,0.5F,1.0F);
					}
				}
			}
			
			//Work out alpha context
			if (m.alpha!=1.0f||m.alphacolor.a!=1.0f) { 
				if (m.alphacolor.a!=1.0f) { //Poser
					m.alpha=m.alphacolor.a;
				} else { //Max
					m.alpha=1.0f-m.alpha;
				}
			} else { //Sketchup test
				if (m.alphacolor.grayscale<1.0f) {
					m.alpha=1.0f-m.alphacolor.grayscale;
				}
			}
			
			//Work out Specular context	
			if (m.specular.grayscale>0.0f) { //has a colour ref for specular different to default
				m.specular=new Color(1.0f-m.specular.r,1.0f-m.specular.g,1.0f-m.specular.b,1.0f); //poser invert color?
				if (m.shininess==2.0f) { //has default shininess
					m.shininess=0.5f;
				} else { //has value could be max
					if (m.shininess>=2.0f) {
						m.specular=new Color(1.0f-m.specular.r,1.0f-m.specular.g,1.0f-m.specular.b,1.0f); //max revert back leave color?
						m.shininess=(Mathf.Clamp(m.shininess,2.0f,10.0f)-2.0f)/7.0f;
					} else { //just in case
						if (m.shininess>=1.00f) {
							m.shininess=Mathf.Clamp(m.shininess,0.0f,1.0f);
						}
					}
				}
			} else {
				if (m.shininess>=2.0F) { //have a shininess yet still have default or missing specular;
					m.shininess=(Mathf.Clamp(m.shininess,2.0f,10.0f)-2.0f)/7.0f; 
					m.specular=new Color(m.shininess,m.shininess,m.shininess,1.0f); //create greyscale specular based up shininess;
				} else { //catch all, has a value between 0 and 2f, force to value between 0-1
					if (m.shininess>=1.00f) {
						m.shininess=Mathf.Clamp(m.shininess,0.0f,1.0f);
					}
				}
			}
			
						
			//Match with Shader
			bool alph=false;
			bool emistxt=false;
			m.ShaderName="";
			if (m.alpha==1.0f&&m.alphaTexPath==null) {
				if (m.emmisiveTexPath!=null) {
					m.ShaderName+="Self-Illumin/";	
					if (TextureList.ContainsKey(m.emmisiveTexPath)) {
						AlphaChannel=TMPTextures[TextureList[m.emmisiveTexPath]];
						emistxt=true;
						alph=true;
					}
				} else {
					if (m.emmisive.grayscale>0.0f) {
						m.ShaderName+="Self-Illumin/";	
					}
				}
			} else {
				if (m.alpha==1.0f) {
					m.ShaderName+="Transparent/Cutout/";
					if (m.alphaTexPath!=null) {
						if (TextureList.ContainsKey(m.alphaTexPath)) {
							AlphaChannel=TMPTextures[TextureList[m.alphaTexPath]];
							alph=true;
						}
					}
				} else {				
					m.ShaderName+="Transparent/";
					if (m.alphaTexPath!=null) {
						if (TextureList.ContainsKey(m.alphaTexPath)) {
							AlphaChannel=TMPTextures[TextureList[m.alphaTexPath]];
							alph=true;
						}
					}
				}
			}
			/* not implemented;
			if (m.bumpTexPath!=null) {
				if (TextureList.ContainsKey(m.bumpTexPath)){
					m.ShaderName+="Bumped ";
					m.BumpTexture=TMPTextures[TextureList[m.bumpTexPath]];
				}
			}
			*/
			if(m.shininess>0.01||m.specularTexPath!=null) {
				m.ShaderName+="Specular";
				if (m.specularTexPath!=null) {
					if (!alph&&TextureList.ContainsKey(m.specularTexPath)) {
						AlphaChannel=TMPTextures[TextureList[m.specularTexPath]];
						alph=true;
					}
				}
			} else {
				m.ShaderName+="Diffuse";
			}
			if (m.diffuseTexPath!=null) {
				if (TextureList.ContainsKey(m.diffuseTexPath)) {			
					m.DiffTexture=TMPTextures[TextureList[m.diffuseTexPath]];
				}
			}
		
			// Process Required Textures
			Texture2D Temp;
			if (alph) {	
				if (m.DiffTexture!=null) {
					AlphaChannel=ScaleTexture(AlphaChannel,m.DiffTexture.width,m.DiffTexture.height);
					Temp=new Texture2D(m.DiffTexture.width,m.DiffTexture.height,TextureFormat.ARGB32,false);
				} else {
					m.DiffTexture=new Texture2D(AlphaChannel.width,AlphaChannel.height,TextureFormat.ARGB32,false);
					Temp=new Texture2D(AlphaChannel.width,AlphaChannel.height,TextureFormat.ARGB32,false);
				}
				src=AlphaChannel.GetPixels(0);
				dest=m.DiffTexture.GetPixels(0);
				tmp=Temp.GetPixels(0);
				int px=0;
				if (!emistxt) {
					if (AlphaChannel.format==TextureFormat.ARGB32) {
						if (m.diffuseTexPath==null) {
							for(px=0; px<tmp.Length; px++) {
								tmp[px] = new Color(0.5F,0.5F,0.5F,src[px].a);
							}
						} else {
							for(px=0; px<tmp.Length;px++) {
								tmp[px] = new Color(dest[px].r,dest[px].g,dest[px].b,src[px].a);
							}						
						}
					} else {
						if (m.diffuseTexPath==null) {
							for(px=0; px<tmp.Length;px++) {
								tmp[px] = new Color(0.5F,0.5F,0.5F,src[px].grayscale);
							}
						} else {
							for(px=0; px<tmp.Length;px++) {
								tmp[px] = new Color(dest[px].r,dest[px].g,dest[px].b,src[px].grayscale);
							}
						}
					}
					Temp.SetPixels(tmp,0);
					m.DiffTexture=Temp;
					m.DiffTexture.SetPixels(m.DiffTexture.GetPixels()); 
					m.DiffTexture.Apply(true);
					m.DiffTexture.Compress(true);
				} else {
					if (AlphaChannel.format==TextureFormat.ARGB32) {
						for(px=0; px<tmp.Length; px++) {tmp[px] = new Color(1.0F,1.0F,1.0F,src[px].a);}
					} else {
						for(px=0; px<tmp.Length; px++) {tmp[px] = new Color(1.0F,1.0F,1.0F,src[px].grayscale);}
					}
					Temp.SetPixels(tmp,0);
					m.EmmisiveTexture=Temp;
					m.EmmisiveTexture.SetPixels(m.EmmisiveTexture.GetPixels());
					m.EmmisiveTexture.Apply(true);
					m.EmmisiveTexture.Compress(true);
				}
			} else {
				if (m.DiffTexture!=null) { 
					Temp=new Texture2D(m.DiffTexture.width,m.DiffTexture.height,TextureFormat.ARGB32,false);
					tmp=m.DiffTexture.GetPixels(0);
					Temp.SetPixels(tmp,0);
					m.DiffTexture=Temp;
					m.DiffTexture.SetPixels(m.DiffTexture.GetPixels());
					m.DiffTexture.Apply(true);
					m.DiffTexture.Compress(true);
				}
			}
			/*  Not Implemented
			if (m.BumpTexture!=null) {			
				m.BumpTexture=NormalMap(m.BumpTexture,3.5f);
				m.BumpTexture.filterMode=FilterMode.Trilinear;
				m.BumpTexture.Compress(true);
			}
			*/
		}
	}
		
	private Material GetMaterial(MaterialData md) {
		Material m;		
		m=new Material(Shader.Find(md.ShaderName));
	
		m.SetColor("_Color", md.diffuse);
		if (md.ShaderName.Contains("Self-Illumin")) {
			if(md.EmmisiveTexture!=null) m.SetTexture("_Illum",md.EmmisiveTexture);
			if (md.emmisive.grayscale>0.0f) {
				m.SetColor("_Color", md.emmisive);
			}
		}
		if (md.ShaderName.Contains("Transparent")) {
			m.SetColor("_Color", new Color(md.diffuse.r,md.diffuse.g,md.diffuse.b,md.alpha));
		}
		if (md.ShaderName.Contains("Cutout")) {
			m.SetFloat("_Cutoff",0.5F);
		}
		if (md.ShaderName.Contains("Specular")) {
			m.SetColor("_SpecColor", md.specular);
			m.SetFloat("_Shininess", 0.01f+(md.shininess-(0.01f*md.shininess)));
		}
		if(md.DiffTexture!=null) m.SetTexture("_MainTex",md.DiffTexture);
		//if(md.BumpTexture!=null) m.SetTexture("_BumpMap",md.BumpTexture); //Not Implemented
		return m;
	}

	/////////////////////////////////////////////BUILD 
	
	private void Build() {
		Dictionary<string, Material> materials = new Dictionary<string, Material>();
		if(hasmaterials) {
			foreach(MaterialData md in materialData) {
				 if (!materials.ContainsKey(md.ID)) {
					materials.Add(md.ID, GetMaterial(md));
				 }
			}
		} 
		GameObject[] ms = new GameObject[buffer.numObjects];
		if(buffer.numObjects == 1) {
			if (!treatasoneobject) { //incase of one geometry but instantiated 
				GameObject go = new GameObject();
				go.transform.parent = gameObject.transform;
				go.AddComponent(typeof(MeshFilter));
				go.AddComponent(typeof(MeshRenderer));
				go.name="_spc_rename_";
				ms[0] = go;
			} else {
				gameObject.AddComponent(typeof(MeshFilter));
				gameObject.AddComponent(typeof(MeshRenderer));
				ms[0] = gameObject;
			}
		} else if(buffer.numObjects > 1) {
			for(int i = 0; i < buffer.numObjects; i++) {
				GameObject go = new GameObject();
				go.transform.parent = gameObject.transform;
				go.AddComponent(typeof(MeshFilter));
				go.AddComponent(typeof(MeshRenderer));
				ms[i] = go;
			}
		}
		buffer.PopulateMeshes(ms, materials);
		
		if (!treatasoneobject) { //Create Heirachy Scene Transfroms
			GameObject[] stgo = new GameObject[sceneTransforms.Count];
			int gi=0;
			
			//InstantiateTheUninstantiatedAndRenameThemAll
			foreach(GameObject Geo in ms) {	
				List<SceneGeo> sgitems = sceneGeo.FindAll(delegate(SceneGeo item){return item.GID == Geo.name; });
				foreach(SceneGeo sg in sgitems) {
					if (sg.isInstance) {
						GameObject go = (GameObject)Instantiate(Geo); 
						go.transform.parent=gameObject.transform;
						go.name=sg.ParentNode;
						Material[] instancemat = new Material[sg.GMaterial.Count];
						for(int i=0;i<sg.GMaterial.Count;++i) {
							instancemat[i] = materials[sg.GMaterial[i].id];
							instancemat[i].name = sg.GMaterial[i].id;
						}
						go.renderer.materials = instancemat;
						stgo[gi]=go;
						++gi;
					} else {
						Geo.name=sg.ParentNode;
						stgo[gi]=Geo;
						++gi;
					}
				}
			}	
			foreach(SceneTransform st in sceneTransforms){
				if (!st.hasgeometry){
					GameObject go = new GameObject();
					go.name=st.NodeName;
					go.transform.parent=gameObject.transform;
					stgo[gi]=go;
					++gi;
				} 	
			}
			foreach(GameObject go in stgo) { //sort Heirachy Scene Transforms
				bool found=false;
				bool toplevel=false;
				SceneTransform stg = sceneTransforms.Find(delegate(SceneTransform item) { return item.NodeName == go.name; }); 
				if (stg.ParentNode!=null) {
					foreach(GameObject go2 in stgo) {
						if (stg.ParentNode==go2.name) {
							go.transform.parent=go2.transform;
							found=true;
						}
					}
					if (!found) {
						go.transform.parent=gameObject.transform;
					}
				} else {
					toplevel=true;
				}
				go.transform.localRotation = stg.rot;
				if (toplevel) {
					go.transform.localPosition = (stg.pos*MasterScale)+(MainOffset*MasterScale);
				} else {
					go.transform.localPosition = stg.pos*MasterScale;
				}
				go.transform.localScale = stg.scl;
			}
		} 
	}
	
	/////////////////////////////////////////////HELPER FUNCTIONS
	
	private float cf(string v) {
			return Convert.ToSingle(v.Trim(), new CultureInfo("en-US"));
	}
	
	private int ci(string v) {
			return Convert.ToInt32(v.Trim(), new CultureInfo("en-US"));
	}
	
	static string RemoveWS(string p) {
		StringBuilder b = new StringBuilder(p);
		b.Replace("\r"," ");
		b.Replace("\n"," ");
		string r=b.ToString();
		r=Regex.Replace(r,@"\s+"," ");
		return r.Trim();
	}
	
	private Texture2D NormalMap(Texture2D source,float strength) {
		strength=Mathf.Clamp(strength,0.0F,10.0F);
		Texture2D result;
		float xLeft;
		float xRight;
		float yUp;
		float yDown;
		float yDelta;
		float xDelta;
		result = new Texture2D (source.width, source.height, TextureFormat.ARGB32, true);
		for (int by=0; by < result.height; by++) {
			for (int bx=0; bx < result.width; bx++) {
					 xLeft = source.GetPixel(bx-1,by).grayscale*strength;
					 xRight = source.GetPixel(bx+1,by).grayscale*strength;
					 yUp = source.GetPixel(bx,by-1).grayscale*strength;
					 yDown = source.GetPixel(bx,by+1).grayscale*strength;
					 xDelta = ((xLeft-xRight)+1)*0.5f;
					 yDelta = ((yUp-yDown)+1)*0.5f;
				result.SetPixel(bx,by,new Color(xDelta,yDelta,1.0f,yDelta));
			}
		}
	  result.Apply();
	  return result;
	}
	
	private Texture2D ScaleTexture(Texture2D source,int targetWidth,int targetHeight) {
		Texture2D result=new Texture2D(targetWidth,targetHeight,source.format,true);
		Color[] rpixels=result.GetPixels(0);
		float incX=((float)1/source.width)*((float)source.width/targetWidth);
		float incY=((float)1/source.height)*((float)source.height/targetHeight);
		for(int px=0; px<rpixels.Length; px++) {	
			rpixels[px] = source.GetPixelBilinear(incX*((float)px%targetWidth),incY*((float)Mathf.Floor(px/targetWidth)));
		}
		result.SetPixels(rpixels,0);
		result.Apply();
		return result;
	}
	
private string ResolvePath(string filepath,string basepath) {
		//Attempts to resolve file paths, through quick search and altering directory separators for different platforms.
		string outpath="";
		if (Application.isWebPlayer||basepath.Contains("http://")) { //dealing with webaddress or can't run File.Exists
			//some kind of web url checker
			//Webplayer needs "file://"
			//WWW texloader = new WWW(basepath + txt.Key);
			//yield return texloader;
			//if (texloader.error != null) {	
		} else {
			//File.Exists doesn't like "file://"?
			filepath=filepath.Replace("file://", "");
			filepath=filepath.Replace("file:\\", "");
			basepath=basepath.Replace("file://", "");
			basepath=basepath.Replace("file:\\", "");
			string sep=Path.DirectorySeparatorChar.ToString();
			string asep;
			if (sep=="\\") {
				asep="/";
			} else {
				asep="\\";
			}
			filepath=filepath.Replace(asep, sep);
			if (System.IO.File.Exists(basepath+filepath)) {
				outpath=basepath+filepath; 
			} else {
				if (System.IO.File.Exists(basepath+Path.DirectorySeparatorChar.ToString()+filepath)) {
					outpath=basepath+Path.DirectorySeparatorChar.ToString()+filepath;
				} else {
					if (System.IO.File.Exists(filepath)) {
						outpath=filepath;
					} else {
						string[] fpp=filepath.Split(sep.ToCharArray());
						string[]	bpp=basepath.Split(sep.ToCharArray());
						bool found=false;
						for (int i=(bpp.Length-1);i>=0;i--) {
							for (int j=(fpp.Length-1);j>=0;j--) {
								if (!found) {
									StringBuilder newpath = new StringBuilder();
									for (int k=0;k<i;k++) {
										newpath.Append(bpp[k]).Append(sep);
									}
									for (int k=j;k<fpp.Length;k++) {
										newpath.Append(fpp[k]).Append(sep);
									}
									newpath.Remove(newpath.Length-1, 1);
									if (System.IO.File.Exists(newpath.ToString())) {
										found=true;
										outpath=newpath.ToString();
										break;
									}					 
								}
							}
							if (found) break;
						}
					}
				}
			}				
			outpath="file://"+outpath;
		}
		return outpath;
	}
}







