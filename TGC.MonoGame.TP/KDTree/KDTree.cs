using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP
{

	// datos en el punto de interseccion
	public struct ip_data
	{
		public Vector3 ip;        // intersection point
		public float t;        // distancia al punto de interseccion
		public float bc_b;     // barycentric B
		public float bc_g;     // barycentric G
		public int nro_face;
	};

	public class kd_node
	{
		public Vector3 p_min;
		public Vector3 p_max;
		public int deep;
		public float split;
		public int split_plane;
		public kd_node p_left, p_right;
		public int cant_f;
		public int[] p_list;

	}


	public struct st_traverse_node
	{
		public float tnear, tfar;
		public kd_node p_nodo;
	};



	public class KDTree
    {
		public const int MAX_FACE_X_NODE = 10;
		public const float EPSILON = 0.00001f;

		public int max_deep;
		public kd_node kd_tree;
		public int cant_faces;
		public bsp_face[] F;
		public Vector3 bb_min;
		public Vector3 bb_max;

		public KDTree( int p_cant_faces , bsp_face []p_faces)
        {
			cant_faces = p_cant_faces;
			F = p_faces;
			precalc();
        }

		public void precalc()
        {
			// precalculos x face
			// bounding box scene
			bb_min.X = 100000;
			bb_min.Y = 100000;
			bb_min.Z = 100000;
			bb_max.X = -100000;
			bb_max.Y = -100000;
			bb_max.Z = -100000;


			for (int i = 0; i < cant_faces; ++i)
			{
				// bounding box face
				float min_x = 100000;
				float min_y = 100000;
				float min_z = 100000;
				float max_x = -100000;
				float max_y = -100000;
				float max_z = -100000;
				for (int j = 0; j < 3; ++j)
				{
					float x = F[i].v[j].X;
					float y = F[i].v[j].Y;
					float z = F[i].v[j].Z;
					if (x < min_x)
						min_x = x;
					if (y < min_y)
						min_y = y;
					if (z < min_z)
						min_z = z;
					if (x > max_x)
						max_x = x;
					if (y > max_y)
						max_y = y;
					if (z > max_z)
						max_z = z;

					// scene bb
					if (x < bb_min.X)
						bb_min.X = x;
					if (y < bb_min.Y)
						bb_min.Y = y;
					if (z < bb_min.Z)
						bb_min.Z = z;
					if (x > bb_max.X)
						bb_max.X = x;
					if (y > bb_max.Y)
						bb_max.Y = y;
					if (z > bb_max.Z)
						bb_max.Z = z;
				}

				F[i].pmin = new Vector3(min_x, min_y, min_z);
				F[i].pmax = new Vector3(max_x, max_y, max_z);

				F[i].e1 = F[i].v[1] - F[i].v[0];
				F[i].e2 = F[i].v[2] - F[i].v[0];
			}
		}


		// chequeo si un bounding box esta dentro de otro(o parcialmente adentro)
		public bool box_overlap(Vector3 Amin, Vector3 Amax, Vector3 Bmin, Vector3 Bmax)
		{
			if (Amin.X > Bmax.X) return false;
			if (Amin.Y > Bmax.Y) return false;
			if (Amin.Z > Bmax.Z) return false;

			if (Amax.X < Bmin.X) return false;
			if (Amax.Y < Bmin.Y) return false;
			if (Amax.Z < Bmin.Z) return false;

			return true;            // overlap
		}


		public kd_node createKDTreeNode(Vector3 pmin, Vector3 pmax, int deep, int cant_f, int []p_list)
        {
			kd_node p_node = new kd_node();
			p_node.deep = deep;
			p_node.p_min = pmin;
			p_node.p_max = pmax;

			// creo un nodo leaf
			p_node.cant_f = cant_f;
			p_node.p_list = p_list;

			// si la cantidad de primitivas en el nodo es mayor a cierto limite y el deep no supera el maximo, pruebo dividir el nodo
			if (cant_f >= MAX_FACE_X_NODE && deep < max_deep)
			{
				// divido el nodo en 2:
				Vector3 dim = pmax - pmin;
				Vector3 Lmin, Lmax, Rmin, Rmax;
				int eje;
				float s;

				// version eje fijo: selecciono el eje en base a la direccion que mas extension tiene
				{
					if (MathF.Abs(dim.Z) >= MathF.Abs(dim.X) && MathF.Abs(dim.Z) >= MathF.Abs(dim.Y))
					{
						eje = 2;            // split Z
						s = (pmin.Z + pmax.Z) * 0.5f;
					}
					else
					if (MathF.Abs(dim.X) >= MathF.Abs(dim.Z) && MathF.Abs(dim.X) >= MathF.Abs(dim.Y))
					{
						eje = 0;            // splite X
						s = (pmin.X + pmax.X) * 0.5f;
					}
					else
					{ 
						eje = 1;            // split Y
						s = (pmin.Y + pmax.Y) * 0.5f;
					}

					//s = best_split(eje,p_node);
				}

				/*
				else
				{
					// version que prueba los 3 ejes: 
					eje = best_split(p_node, &s);
				}
				*/


				p_node.split = s;
				p_node.split_plane = eje;
				Rmin = Lmin = pmin;
				Rmax = Lmax = pmax;

				switch (eje)
				{
					case 0:
						Lmax.X = s;
						Rmin.X = s;
						break;
					case 1:
						Lmax.Y = s;
						Rmin.Y = s;
						break;
					case 2:
						Lmax.Z = s;
						Rmin.Z = s;
						break;
				}

				// clasifico las primitivas
				int cant_L = 0;
				int cant_R = 0;

				int[] list_L = new int[cant_f];
				int[] list_R = new int[cant_f];

				for (int i = 0; i < cant_f; ++i)
				{
					bsp_face f = F[p_list[i]];
					if (box_overlap(f.pmin, f.pmax, Lmin, Lmax))
						list_L[cant_L++] = p_list[i];
					if (box_overlap(f.pmin, f.pmax, Rmin, Rmax))
						list_R[cant_R++] = p_list[i];
				}

				// hago el nodo interior: 
				// libero la memoria original 
				/*
				if (p_node.p_list)
				{
					delete[] p_node.p_list;
					p_node.p_list = NULL;
				}*/
				p_node.p_list = null;

				// creo los 2 nodos hijos
				p_node.p_left = createKDTreeNode(Lmin, Lmax, deep + 1, cant_L, list_L);
				p_node.p_right = createKDTreeNode(Rmin, Rmax, deep + 1, cant_R, list_R);

			}

			return p_node;
		}

		public void createKDTree()
        {
			max_deep = ((int)(8 + 1.3f * (float) Math.Log(cant_faces)));
			// creo un nodo con toda la escena
			int[] p_list = new int[cant_faces];
			for (int i = 0; i < cant_faces; ++i)
				p_list[i] = i;
			kd_tree = createKDTreeNode(bb_min, bb_max, 0, cant_faces, p_list);
		}


		public void deleteKDTreeNode(kd_node p_node)
		{
			if (p_node.p_left!=null)
				deleteKDTreeNode(p_node.p_left);
			if (p_node.p_right!=null)
				deleteKDTreeNode(p_node.p_right);
			if (p_node.p_list!=null)
				p_node.p_list = null;
			//SAFE_DELETE(p_node);
		}


		public int countKDTreeNodes(kd_node p_node)
		{
			int rta = 0;
			if (p_node.p_list!=null)
				rta += p_node.cant_f;
			if (p_node.p_left!=null)
				rta += countKDTreeNodes(p_node.p_left);
			if (p_node.p_right!=null)
				rta += countKDTreeNodes(p_node.p_right);
			return rta;
		}

		public float Vector3Eje(Vector3 v, int i)
        {
			float rta;
			switch(i)
            {
				case 0:
				default:
					rta = v.X;
					break;
				case 1:
					rta = v.Y;
					break;
				case 2:
					rta = v.Z;
					break;
			}

			return rta;
        }
		public bool box_intersection(Vector3 pMin, Vector3 pMax,
					  Vector3 O,   //Ray origin
					  Vector3 D,   //Ray direction
					  out float tn, out float tf,
					  float mint = 0.001f, float maxt = 1000000)
		{

			tn = -1;
			tf = -1;

			float t0 = mint, t1 = maxt;
			for (int i = 0; i < 3; ++i)
			{
				// Update interval for _i_th bounding box slab
				float invRayDir = 1.0f / Vector3Eje(D,i);
				float tNear = (Vector3Eje(pMin,i) - Vector3Eje(O,i)) * invRayDir;
				float tFar = (Vector3Eje(pMax,i) - Vector3Eje(O,i)) * invRayDir;

				// Update parametric interval from slab intersection $t$s
				if (tNear > tFar)
				{
					float aux = tNear;
					tNear = tFar;
					tFar = aux;
				}
				t0 = tNear > t0 ? tNear : t0;
				t1 = tFar < t1 ? tFar : t1;
				if (t0 > t1) 
					return false;
			}
			tn = t0;
			tf = t1;
			return true;
		}

		// version optimizada
		public bool triangle_ray(int i,    // nro de face
									Vector3 O,  //Ray origin
									Vector3 D,  //Ray direction
									out float t_out,
									out float U,
									out float V)
		{
			Vector3 e1 = F[i].e1, e2 = F[i].e2;  //Edge1, Edge2
			Vector3 P, Q, T;
			float det, inv_det, u, v;
			float t;

			t_out = 0;
			U = V = 0;

			//Begin calculating determinant - also used to calculate u parameter
			P = Vector3.Cross(D, e2);
			//if determinant is near zero, ray lies in plane of triangle or ray is parallel to plane of triangle
			det = Vector3.Dot(e1, P);
			//NOT CULLING
			if (det > -EPSILON && det < EPSILON) return false;
			inv_det = 1.0f / det;

			//calculate distance from V1 to ray origin
			T = O - F[i].v[0];

			//Calculate u parameter and test bound
			u = Vector3.Dot(T, P) * inv_det;
			//The intersection lies outside of the triangle
			if (u < 0.0f || u > 1.0f) return false;

			//Prepare to test v parameter
			Q = Vector3.Cross(T, e1);

			//Calculate V parameter and test bound
			v = Vector3.Dot(D, Q) * inv_det;
			//The intersection lies outside of the triangle
			if (v < 0.0f || u + v > 1.0f) return false;

			t = Vector3.Dot(e2, Q) * inv_det;

			if (t > EPSILON)
			{ 
				//ray intersection
				t_out = t;
				U = u;
				V = v;
				return true;
			}

			// No hit, no win
			return false;
		}




		public bool ray_intersects(Vector3 O, Vector3 D, out ip_data I)
		{
			float R = 10000000;
			float bc_b = 0;
			float bc_g = 0;
			int nro_face = -1;

			I = new ip_data();

			// chequeo la interseccion con el bounding box de la escena o 
			if (!box_intersection(bb_min, bb_max, O, D, out float tnear, out float tfar))
				// el rayo no interseca con la escena
				return false;

			// precomputo la inv de direccion del rayo
			float []ray_invdir = { 0, 0, 0 };
			if (Math.Abs(D.X) > 0.00001)
				ray_invdir[0] = 1.0f / D.X;
			if (Math.Abs(D.Y) > 0.00001)
				ray_invdir[1] = 1.0f / D.Y;
			if (Math.Abs(D.Z) > 0.00001)
				ray_invdir[2] = 1.0f / D.Z;
			float []ray_O = { O.X, O.Y, O.Z };
			float []ray_dir = { D.X, D.Y, D.Z };

			// comienzo el traverse con el nodo root = (kd_tree, tnear, tfar)
			kd_node p_node = kd_tree;
			// pila de pendientes
			int p_stack = 0;
			st_traverse_node []S = new st_traverse_node[64];

			while (p_node != null)
			{
				// el rayo atraviesa el nodo p_node entrando por tnear y saliendo por tfar. 
				if (p_node.p_list!=null)
				{
					// nodo hoja: chequeo la interseccion con la lista de caras en dicho nodo
					for (int i = 0; i < p_node.cant_f; ++i)
					{
						int n = p_node.p_list[i];
                        if (triangle_ray(n, O, D, out float t, out float b, out float g))
						{
							if (t > 1 && t < R && t >= tnear - 1 && t <= tfar + 1)
							{
								// actualizo las coordenadas barycentricas
								bc_b = b;
								bc_g = g;
								R = t;
								nro_face = n;
							}
						}
					}

					// early termination
					if (nro_face != -1)
					{
						I.ip = O + D * R;
						I.t = R;
						I.bc_b = bc_b;
						I.bc_g = bc_g;
						I.nro_face = nro_face;
						return true;
					}

					// termine de procesar la rama (llegue a un nodo hoja). Si tengo algo pendiente en la pila, lo saco de ahi
					if (p_stack > 0)
					{
						p_stack--;
						p_node = S[p_stack].p_nodo;
						tnear = S[p_stack].tnear;
						tfar = S[p_stack].tfar;
					}
					else
						p_node = null;          // termino

				}
				else
				{
					// si es un nodo interior: 
					// determino en que orden tengo que chequear los nodos hijos 
					int p = p_node.split_plane;
					float tplane = (p_node.split - ray_O[p]) * ray_invdir[p];

					kd_node p_near, p_far;
					if (ray_O[p] <= p_node.split || (ray_O[p] == p_node.split && ray_dir[p] <= 0))
					{
						// proceso primero el Left node y luego el Right node
						p_near = p_node.p_left;
						p_far = p_node.p_right;
					}
					else
					{
						// proceso primero el Right node y luego el Left node
						p_near = p_node.p_right;
						p_far = p_node.p_left;
					}

					// para procesar ambos nodos el tplane tiene que estar entre tnear y tfar
					if (tplane > tfar || tplane <= 0)
					{
						// el rayo solo pasa por el primer nodo (el nodo cercano) : avanzo hacia el nodo cercano
						p_node = p_near;
					}
					else
					if (tplane < tnear)
					{
						// el rayo solo pasa por el segundo nodo (el nodo lejano) : avanzo hacia el nodo lejano
						p_node = p_far;
					}
					else
					{
						// pasa por ambos nodos: 

						// tengo que evaluar el segundo nodo luego del primero, asi que lo pongo en la pila de pendientes
						// el nodo far va desde tplane hasta tfar
						S[p_stack].p_nodo = p_far;
						S[p_stack].tnear = tplane;
						S[p_stack].tfar = tfar;
						p_stack++;

						//if(p_stack>max_todo)
						//max_todo =p_stack;

						// a continuacion proceso el nodo  cercano: que va desde tnear, hasta tplane
						p_node = p_near;
						tfar = tplane;
						// tnear queda como esta
					}
				}
			}

			return false;
		}


	}
}
