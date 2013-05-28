/*!
** Copyright (c) 2011 
**
** Jon Martin of www.jon-martin.com & www.fusedworks.com
** MIT copyright also to Bartek Drodz of www.everyday3d.com for initial outline scripts
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
using System.Text;

public class OBJ : MonoBehaviour {
	
	public string objPath;
	public float objmaxsize=100.0F; //never negative
	public float objminsize=0.0F; //never negative
	public bool EnforceSingleObj=false;
	public float BumpStrength=1.8F;
	public float MasterScale = 1.0F;
	public String[] Shaders = new String[0];
	//public bool AutoResolveVLimit=true; //not implemented yet
	
	private scaleoffset mainso; 
	private scaleoffset currentso; 
	private List<scaleoffset> objso=new List<scaleoffset>(); 
	private class scaleoffset{
		public string name;
		public bool initval=false;
		public float MasterScale=1.0F;
		public float minvpos;
		public float maxvpos;
		public float minvxpos;
		public float maxvxpos;
		public float minvypos;
		public float maxvypos;
		public float minvzpos;
		public float maxvzpos;
		public float MasterOffsetx=0;
		public float MasterOffsety=0;
		public float MasterOffsetz=0;
		public Vector3 MainOffset=new Vector3();
		public int vfirst;
		public int vlast;
	}
	
	private bool treatasoneobject=false;
	private int vcount=0;
	
	/* OBJ file tags */
	private const string O 	= "o";
	private const string G 	= "g";
	private const string V 	= "v";
	private const string VT = "vt";
	private const string VN = "vn";
	private const string F 	= "f";
	private const string MTL = "mtllib";
	private const string UML = "usemtl";

	/* MTL file tags */
	private const string NML = "newmtl";
	private const string NS = "Ns"; // Shininess
	private const string KA = "Ka"; // Ambient component (not supported)
	private const string KD = "Kd"; // Diffuse component
	private const string KE = "Ke"; // Emissive / Self Illum component
	private const string KS = "Ks"; // Specular component
	private const string D = "d"; 	// Transparency (not supported)
	private const string TR = "Tr";	// Same as 'd'
	private const string ILLUM = "illum"; // Illumination model. 1 - diffuse, 2 - specular
	private const string MAP_KA = "map_Ka"; // Ambient texture //Not Implemented 
	private const string MAP_KD = "map_Kd"; // Diffuse texture 
	private const string MAP_KS = "map_Ks"; // Specular texture 
	private const string MAP_KE = "map_Ke"; // Self-Illumination or Emmisive texture
	private const string MAP_D = "map_d"; // Alpha texture or -
	private const string MAP_TR = "map_tr"; //  Alpha texture 
	private const string MAP_BUMP = "map_bump"; // Bump texture (Reflection, Refraction,Displacement textures are not supported currently)

	private string basepath;
	private string mtllib;
	
	private GeometryBuffer buffer;
	private Texture2D[] TMPTextures;

	///Initialiser
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
			SetGeometryData(loader.text);
		}
		
		if (buffer.Check(true,false)) { //buffer.Trace(AutoResolveVLimit)
			if(hasMaterials) {
				loader = new WWW(ResolvePath(mtllib,basepath));
				yield return loader;
				if (loader.error != null) {
					Debug.Log("failed to resolve material library path"+basepath+mtllib);  //!!!!!
					string[] emats=buffer.ReturnMaterialNames(); 
					materialData = new List<MaterialData>();
					MaterialData nmd = new MaterialData();
					foreach(string mname in emats) {
						nmd = new MaterialData();
						nmd.name = mname;
						nmd.ShaderName="Diffuse";
						nmd.diffuse = new Color(0.5f, 0.5f, 0.5f, 1.0f);
						materialData.Add(nmd);
					}
				} else {
					SetMaterialData(loader.text);						
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
				}	
			} 
			CheckScale();
			Build();
		} else {
			Debug.Log("Too many poly's");
		}
		Destroy (this);
	}
	//Set ScaleOffset data for object or master group
	private void pushscaleoffset(scaleoffset so,Vector3 v) {
		if (!so.initval) {
			so.minvxpos=v.x;
			so.maxvxpos=v.x;
			so.minvypos=v.y;
			so.maxvypos=v.y;
			so.minvzpos=v.z;
			so.maxvzpos=v.z;
			so.initval=true;
		} else {
			so.minvxpos=v.x<so.minvxpos?v.x:so.minvxpos;
			so.maxvxpos=v.x>so.maxvxpos?v.x:so.maxvxpos;
			so.minvypos=v.y<so.minvypos?v.y:so.minvypos;
			so.maxvypos=v.y>so.maxvypos?v.y:so.maxvypos;
			so.minvzpos=v.z<so.minvzpos?v.z:so.minvzpos;
			so.maxvzpos=v.z>so.maxvzpos?v.z:so.maxvzpos;
		}
	}
	///Geometry
	private void SetGeometryData(string data) {
		data = data.Replace("\r\n","\n");
		string[] lines = data.Split("\n".ToCharArray()); 
		Vector3 v;
		mainso = new scaleoffset();
		for(int i = 0; i < lines.Length; i++) {
			string l = lines[i];
			l=Regex.Replace(l,@"# object","o");//tomekkie ALTERATION
			if(l.IndexOf("#") != -1) l = l.Substring(0, l.IndexOf("#"));
			l=Regex.Replace(l,@"\s+"," ");
			l=l.Trim();
			string[] p = l.Split(" ".ToCharArray());  
			switch(p[0]) {
				case O:
					if (!EnforceSingleObj) {
						if (currentso!=null) {
							currentso.vlast=vcount;
							objso.Add(currentso);
						}
						buffer.PushObject(p[1].Trim());
						currentso = new scaleoffset();
						currentso.vfirst=vcount;
						currentso.name=p[1].Trim();
					}
					break;
				case G:
					buffer.PushGroup(p[1].Trim());
					break;
				case V:		
					if (p.Length>=3) {
						v=new Vector3(cf(p[1]),cf(p[2]),0-cf(p[3]));
						buffer.PushVertex(v);    //Any 0- Flipping should match normals
						vcount++;
						pushscaleoffset(mainso,v);
						if (currentso!=null) {
							pushscaleoffset(currentso,v);
						}
					}
					break;
				case VT:
					if (p.Length>=2) {
						buffer.PushUV(new Vector2( cf(p[1]), cf(p[2]) ));
					}
					break;
				case VN:
					if (p.Length>=3) {
						buffer.PushNormal(new Vector3(cf(p[1]),cf(p[2]),0-cf(p[3]))); //Any 0- Flipping should match vertex
					}
					break;
				case F:
					if (p.Length>=4) { 
						if (p.Length<=5) {//is triangle or quad
							string[] c;
							for (int j=0;j<p.Length-3;j++) {	//get all possible triangles from line
								FaceIndices fi = new FaceIndices();	//1 vert
									c=p[1].Trim().Split("/".ToCharArray());
									if (c.Length > 0 && c[0] != string.Empty) {fi.vi = ci(c[0])-1;}
									if (c.Length > 1 && c[1] != string.Empty) {fi.vu = ci(c[1])-1;}
									if (c.Length > 2 && c[2] != string.Empty) {fi.vn = ci(c[2])-1;}
								buffer.PushFace(fi);
								for (int k=0;k<2;k++) {	//2nd and 3rd vert
									fi = new FaceIndices();	
										int no=3-k+j; //		To invert faces replace with : int no=2+k+j;
										c=p[no].Trim().Split("/".ToCharArray());
										if (c.Length > 0 && c[0] != string.Empty) {fi.vi = ci(c[0])-1;}
										if (c.Length > 1 && c[1] != string.Empty) {fi.vu = ci(c[1])-1;}
										if (c.Length > 2 && c[2] != string.Empty) {fi.vn = ci(c[2])-1;}
									buffer.PushFace(fi);
								}
							}
						} else { //is poly try triangulate, see TriPoly script
							TriPoly triangulation;
							triangulation = new TriPoly();
							Vector3[] pointlist=new Vector3[p.Length-1];
							//Vector3[] normallist=new Vector3[p.Length-1];
							string[] c;
							for (int j=1;j<p.Length;j++) { //go through each faceindex in poly list and pull relevant vertice from geometrybuffer add to vector[]
								c=p[j].Trim().Split("/".ToCharArray());
								if (c.Length > 0 && c[0] != string.Empty) {pointlist[j-1] = buffer.vertices[ci(c[0])-1];}
								//if (c.Length > 2 && c[2] != string.Empty) {normallist[j-1] = buffer.normals[ci(c[2])-1];}
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
										c=p[indices[j]+1].Trim().Split("/".ToCharArray());
										if (c.Length > 0 && c[0] != string.Empty) {fi.vi = ci(c[0])-1;}
										if (c.Length > 1 && c[1] != string.Empty) {fi.vu = ci(c[1])-1;}
										if (c.Length > 2 && c[2] != string.Empty) {fi.vn = ci(c[2])-1;}
									buffer.PushFace(fi);
								}
							} 
						}
					}
					break;
				case MTL:
					mtllib = p[1].Trim();
					break;
				case UML:
					buffer.PushMaterialGroup(p[1].Trim()); //hello
					break;
			}
		}
		if (currentso!=null) {
			currentso.vlast=vcount;
			objso.Add(currentso);
		}
	}
	
	private void CalculateScaleOffset(scaleoffset so,bool above){
		so.MasterOffsetx=0.0f-((so.maxvxpos/2.0f)+(so.minvxpos/2.0f));
		so.MasterOffsety=0.0f-((so.maxvypos/2.0f)+(so.minvypos/2.0f));
		so.MasterOffsetz=0.0f-((so.maxvzpos/2.0f)+(so.minvzpos/2.0f));
		//if (above) { //UPrestingonzero
		//	so.MainOffset=new Vector3(so.MasterOffsetx,so.MasterOffsety+((so.maxvypos-so.minvypos)/2.0f),so.MasterOffsetz);
		//} else { //UpCentred
			so.MainOffset=new Vector3(so.MasterOffsetx,so.MasterOffsety,so.MasterOffsetz);
		//}
		so.minvpos=so.minvypos<so.minvxpos?so.minvypos:so.minvxpos;
		so.minvpos=so.minvzpos<so.minvpos?so.minvzpos:so.minvpos;
		so.maxvpos=so.maxvypos>so.maxvxpos?so.maxvypos:so.maxvxpos;
		so.maxvpos=so.maxvzpos>so.maxvpos?so.maxvzpos:so.maxvpos;
		float ep=0.0F;
		ep=(0-so.minvpos)>so.maxvpos?(0-so.minvpos):so.maxvpos;
		/*if (ep!=0.0f) {
			if (ep>objmaxsize) so.MasterScale=objmaxsize/ep;
			if (ep<objminsize) so.MasterScale=objminsize/ep;
		} else {
			so.MasterScale=1.0f;
		}*/
		so.MasterScale = MasterScale;
	}
	
	private void CheckScale() {
		CalculateScaleOffset(mainso,true);
		if (!EnforceSingleObj && objso.Count>1) {
			treatasoneobject=false;
			foreach(scaleoffset so in objso) {
				CalculateScaleOffset(so,false);
				for(int i=so.vfirst;i<so.vlast;i++) {
					buffer.vertices[i]=(buffer.vertices[i]+so.MainOffset)*mainso.MasterScale;
				}
			}
		} else {
			treatasoneobject=true;
			for(int i=0;i<buffer.vertices.Count;i++) {
				buffer.vertices[i]=(buffer.vertices[i]+mainso.MainOffset)*mainso.MasterScale;
			}
		}
	}

	private float cf(string v) {
			return Convert.ToSingle(v.Trim(), new CultureInfo("en-US"));
	}
	
	private int ci(string v) {
			return Convert.ToInt32(v.Trim(), new CultureInfo("en-US"));
	}
	
	///Materials
	private Color gc(string[] p) {
		return new Color( cf(p[1]), cf(p[2]), cf(p[3]) );
	}
	
	private bool hasMaterials {
		get {
			return mtllib != null;
		}
	}

	Dictionary<string, int> TextureList = new Dictionary<string, int>();  
	private List<MaterialData> materialData;
	private class MaterialData {
		public string name;
		public Color ambient;
   		public Color diffuse;
   		public Color specular;
		public Color emmisive;
		public bool emmision=false;
   		public float shininess;
   		public float alpha=1.0f;
   		public int illumType;
		public string ambientTexPath;
   		public string diffuseTexPath;
		public string emmisiveTexPath;
		public string specularTexPath;
		public string alphaTexPath;
		public string bumpTexPath;
		public Texture2D DiffTexture; 
		public Texture2D BumpTexture;
		public Texture2D EmmisiveTexture;
		public string ShaderName;
	}
	
	private void SetMaterialData(string data) {
		data = data.Replace("\r\n","\n");
		string[] lines = data.Split("\n".ToCharArray());		
		materialData = new List<MaterialData>();
		MaterialData current = new MaterialData();
		int texturecount=0;		
		for(int i = 0; i < lines.Length; i++) {
			string l = lines[i];			
			if(l.IndexOf("#") != -1) l = l.Substring(0, l.IndexOf("#"));
			l=Regex.Replace(l,@"\s+"," ");
			l=l.Trim();			
			string[] p = l.Split(" ".ToCharArray());
			switch(p[0]) {
				case NML:
					current = new MaterialData();
					current.name = p[1].Trim();
					materialData.Add(current);
					break;
				case KA:
					current.ambient = gc(p);
					break;
				case KD:
					current.diffuse = gc(p);
					break;
				case KS:
					current.specular = gc(p);
					break;
				case KE:
					current.emmisive = gc(p);
					if (current.emmisive.grayscale>0.0f) {
						current.emmision=true;
					}
					break;
				case NS:
					current.shininess = cf(p[1]) / 1000;
					break;
				case D:
					current.alpha = 1.0f-cf(p[1]);
					break;
				case TR:
					current.alpha = 1.0f-cf(p[1]);
					break;
				case MAP_KA: //ambiant - not currently utilised
					current.ambientTexPath = p[p.Length-1].Trim();
					if (!TextureList.ContainsKey(p[p.Length-1].Trim())) { 
						TextureList.Add(p[p.Length-1].Trim(), texturecount);
						texturecount++;
					}
					break;
				case MAP_KD:  //diffuse
					current.diffuseTexPath = p[p.Length-1].Trim();
					if (!TextureList.ContainsKey(p[p.Length-1].Trim())) { 
						TextureList.Add(p[p.Length-1].Trim(), texturecount);
						texturecount++;
					}
					break;
				case MAP_KE: //emmisive
					current.emmisiveTexPath = p[p.Length-1].Trim();
					if (!TextureList.ContainsKey(p[p.Length-1].Trim())) { 
						TextureList.Add(p[p.Length-1].Trim(), texturecount);
						texturecount++;
					}
					break;
				case MAP_KS: //specular
					current.specularTexPath = p[p.Length-1].Trim();
					if (!TextureList.ContainsKey(p[p.Length-1].Trim())) { 
						TextureList.Add(p[p.Length-1].Trim(), texturecount);
						texturecount++;
					}
					break;
				case MAP_D: //alpha 
					current.alphaTexPath = p[p.Length-1].Trim();
					if (!TextureList.ContainsKey(p[p.Length-1].Trim())) { 
						TextureList.Add(p[p.Length-1].Trim(), texturecount);
						texturecount++;
					}
					break;
				case MAP_TR: //alpha
					current.alphaTexPath = p[p.Length-1].Trim();
					if (!TextureList.ContainsKey(p[p.Length-1].Trim())) { 
						TextureList.Add(p[p.Length-1].Trim(), texturecount);
						texturecount++;
					}
					break;
				case MAP_BUMP: //bump
					current.bumpTexPath = p[p.Length-1].Trim();
					if (!TextureList.ContainsKey(p[p.Length-1].Trim())) { 
						TextureList.Add(p[p.Length-1].Trim(), texturecount);
						texturecount++;
					}
					break;
				case ILLUM:
					current.illumType = ci(p[1]);
					break;
			}
		}	
	}
	
	
	private void SolveMaterials() {
		Color[] src;
		Color[] dest;
		Color[] tmp;
		int counter = 0;
		foreach(MaterialData m in materialData) { 
			Texture2D AlphaChannel=new Texture2D(2,2);
			//Match with Shader
			bool alph=false;
			bool emistxt=false;
			m.ShaderName="";
			if (m.alpha==1.0&&m.alphaTexPath==null) {
				if (m.emmision||m.emmisiveTexPath!=null) {
					m.ShaderName+="Self-Illumin/";
					if (m.emmisiveTexPath!=null) {
						if (TextureList.ContainsKey(m.emmisiveTexPath)) {
							AlphaChannel=TMPTextures[TextureList[m.emmisiveTexPath]];
							emistxt=true;
							alph=true;
						}
					}
				}
			} else {
				if (m.alpha==1.0) {
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
			if (m.bumpTexPath!=null) {
				if (TextureList.ContainsKey(m.bumpTexPath)){
					m.ShaderName+="Bumped ";
					m.BumpTexture=TMPTextures[TextureList[m.bumpTexPath]];
				}
			}
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
			if (counter < Shaders.Length)
			    m.ShaderName = Shaders[counter];
			else
				m.ShaderName = "Diffuse";
			counter += 1;
			
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
			if (m.BumpTexture!=null) {			
				m.BumpTexture=NormalMap(m.BumpTexture,BumpStrength);
				m.BumpTexture.filterMode=FilterMode.Trilinear;
				m.BumpTexture.Compress(true);
			}
		}
	}
	
	
	private Material GetMaterial(MaterialData md) {
		Material m;		
		m=new Material(Shader.Find(md.ShaderName));
		m.SetColor("_Color", md.diffuse);
		if (md.ShaderName.Contains("Self-Illumin")) {
			if(md.EmmisiveTexture!=null) m.SetTexture("_Illum",md.EmmisiveTexture);
			if (md.emmision) {
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
			m.SetFloat("_Shininess", md.shininess);
		}
		if(md.DiffTexture!=null) m.SetTexture("_MainTex",md.DiffTexture);
		if(md.BumpTexture!=null) m.SetTexture("_BumpMap",md.BumpTexture);
		return m;
	}

	///Assemble
	private void Build() {
		Dictionary<string, Material> materials = new Dictionary<string, Material>();
		if(hasMaterials) {
			Material m =  new Material(Shader.Find("Diffuse"));
			m.SetColor("_Color", new Color(0.5f, 0.5f, 0.5f, 1.0f)); 
			materials.Add("_spc_default", m); 
			foreach(MaterialData md in materialData) {
				if (!materials.ContainsKey(md.name)) {
					materials.Add(md.name, GetMaterial(md));
				}
			}
		} else {
			Material m =  new Material(Shader.Find("Diffuse"));
			m.SetColor("_Color", new Color(0.5f, 0.5f, 0.5f, 1.0f)); 
			materials.Add("_spc_default", m); 
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
		if (!treatasoneobject) {
			foreach(GameObject go in ms) {
				scaleoffset so = objso.Find(delegate(scaleoffset item) { return item.name == go.name; }); 
				if (so!=null) {
					go.transform.localPosition = ((Vector3.zero-so.MainOffset)*mainso.MasterScale)-((Vector3.zero-mainso.MainOffset)*mainso.MasterScale);
				}
			}
		}
	}

	/////////////////////////////////////////////HELPER FUNCTIONS
	
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







