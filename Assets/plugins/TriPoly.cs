// Triangulate Poly Adapted from C++ for Unity3d Runtime Lib/.Net by Jon-Martin Feb-2011 mailto:info@jon-martin.com 

/* Copyright (c) 2009 by John W. Ratcliff mailto:jratcliffscarab@gmail.com
**
** Portions of this source has been released with the PhysXViewer application, as well as
** Rocket, CreateDynamics, ODF, and as a number of sample code snippets.
**
** If you find this code useful or you are feeling particularily generous I would
** ask that you please go to http://www.amillionpixels.us and make a donation
** to Troy DeMolay.
**
** DeMolay is a youth group for young men between the ages of 12 and 21.
** It teaches strong moral principles, as well as leadership skills and
** public speaking.  The donations page uses the 'pay for pixels' paradigm
** where, in this case, a pixel is only a single penny.  Donations can be
** made for as small as $4 or as high as a $100 block.  Each person who donates
** will get a link to their own site as well as acknowledgement on the
** donations blog located here http://www.amillionpixels.blogspot.com/
**
** If you wish to contact me you can use the following methods:
**
** Skype ID: jratcliff63367
** Yahoo: jratcliff63367
** AOL: jratcliff1961
** email: jratcliffscarab@gmail.com
**
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

public class TriPoly {
	
	Vector3[] mInputPoints;
	Vector3[] mPoints;
	List<int> mIndices=new List<int>();
	int[] Indices;
	int[] V;
	
	public int[] Patch(Vector3[] Points) { //Would be nice if face direction could be achieved by utilising face normals.
		mInputPoints=Points;
		Main();
		return Indices;
	}
	
	private bool Main() {
		bool success=false;
        if (mInputPoints!=null){
			if (mInputPoints.Length>2){
				success=true;
				Vector3 mMin = mInputPoints[0];
				Vector3 mMax = mInputPoints[0];
				Vector3 v;
				for(int i=0;i<mInputPoints.Length;i++) {
					v=mInputPoints[i];
					if ( v.x < mMin.x ) mMin.x = v.x;
					if ( v.y < mMin.y ) mMin.y = v.y;
					if ( v.z < mMin.z ) mMin.z = v.z;
					if ( v.x > mMax.x ) mMax.x = v.x;
					if ( v.y > mMax.y ) mMax.y = v.y;
					if ( v.z > mMax.z ) mMax.z = v.z;
				}
				// locate the first, second and third longest edges and store them in i1, i2, i3
				float dx = mMax.x - mMin.x; 
				float dy = mMax.y - mMin.y;
				float dz = mMax.z - mMin.z;
				int i1,i2,i3;
				if (dx>dy&&dx>dz) {
					i1 = 0;
					if (dy>dz) {
						i2 = 1;
						i3 = 2;
					} else {
						i2 = 2;
						i3 = 1;
					}
				} else if (dy>dx&&dy>dz) {
					i1 = 1;
					if (dx>dz) {
						i2 = 0;
						i3 = 2;
					} else {
						i2 = 2;
						i3 = 0;
					}
				} else {
					i1 = 2;
					if (dx>dy) {
						i2 = 0;
						i3 = 1;
					} else {
						i2 = 1;
						i3 = 0;
					}
				}
				mPoints=new Vector3[mInputPoints.Length];
				Vector3 temp=new Vector3();
				for(int i=0;i<mInputPoints.Length;i++) {
					temp=mInputPoints[i];
					mPoints[i]=new Vector3(temp[i1],temp[i2],temp[i3]);
				}
				Triangulation();
				if (mIndices.Count>2) {
					Indices=new int[mIndices.Count];
					for(int i=0;i<mIndices.Count;i++) {
						Indices[i]=mIndices[i];
					}
				} else {
					success=false;
					Indices=new int[1];
					Indices[0]=-1;
				}
			}
        }	
		return success;
    }
	
	private void Triangulation() {
		int n=mPoints.Length;
		V=new int[n];
		bool flipped=false;
		if (0.0f < _area()) {
			for (int v=0; v<n; v++) {
				V[v]=v;
			}
		} else {
			flipped=true;
			for (int v=0; v<n; v++) {
				V[v]=(n-1)-v;
			}
		}
		int nv=n;
		int count=2*nv;
		for (int m=0, v=nv-1; nv>2;) {
			if (0 >= (count--)) return;
			int u=v;
			if (nv<=u) u=0;
			v=u+1;
			if (nv<=v) v=0;
			int w=v+1;
			if (nv<=w) w=0;
			if (_snip(u, v, w, nv)) {
				int a, b, c, s, t;
				a = V[u];
				b = V[v];
				c = V[w];
				if (flipped) {
					mIndices.Add(a);
					mIndices.Add(b);
					mIndices.Add(c);
				} else {
					mIndices.Add(c);
					mIndices.Add(b);
					mIndices.Add(a);
				}
				m++;
				for (s=v, t=v+1; t<nv; s++, t++) {
					V[s] = V[t];
				}
				nv--;
				count=2*nv;
			}
		}
	}
	
	private float _area() {
		int n = mPoints.Length;
		float A = 0.0f;
		Vector3 pval;
		Vector3 qval;
		for (int p=n-1, q=0; q<n; p=q++) {
			pval = mPoints[p];
			qval = mPoints[q];
			A += pval.x*qval.y - qval.x*pval.y;
		}
		return (A * 0.5f);
	}

	private bool _snip(int u, int v, int w, int n) {
		int p;
		Vector3 A=mPoints[V[u]];
		Vector3 B=mPoints[V[v]];
		Vector3 C=mPoints[V[w]];
		if (Mathf.Epsilon > (((B.x-A.x)*(C.y-A.y))-((B.y-A.y)*(C.x-A.x))) ) return false;
		for (p=0; p<n; p++) {
			if ((p == u) || (p == v) || (p == w)) continue;
			Vector3 P = mPoints[V[p]];
			if (_insideTriangle(A, B, C, P)) return false;
		}
		return true;
	}

	private bool _insideTriangle(Vector3 A,Vector3 B,Vector3 C,Vector3 P) {
		float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
		float cCROSSap, bCROSScp, aCROSSbp;
		ax = C.x - B.x;  ay = C.y - B.y;
		bx = A.x - C.x;  by = A.y - C.y;
		cx = B.x - A.x;  cy = B.y - A.y;
		apx = P.x - A.x;  apy = P.y - A.y;
		bpx = P.x - B.x;  bpy = P.y - B.y;
		cpx = P.x - C.x;  cpy = P.y - C.y;

		aCROSSbp = ax * bpy - ay * bpx;
		cCROSSap = cx * apy - cy * apx;
		bCROSScp = bx * cpy - by * cpx;
		return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
	}

}
