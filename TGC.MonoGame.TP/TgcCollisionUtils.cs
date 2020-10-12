using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using System;
using System.ComponentModel;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace TGC.MonoGame.TP
{
	public class TgcCollisionUtils
	{

		/// <summary>
		///     Detecta colision entre un segmento pq y un triangulo abc.
		///     Devuelve true si hay colision y carga las coordenadas barycentricas (u,v,w) de la colision, el
		///     instante t de colision y el punto c de colision.
		///     Basado en: Real Time Collision Detection pag 191
		/// </summary>
		/// <param name="p">Inicio del segmento</param>
		/// <param name="q">Fin del segmento</param>
		/// <param name="a">Vertice 1 del triangulo</param>
		/// <param name="b">Vertice 2 del triangulo</param>
		/// <param name="c">Vertice 3 del triangulo</param>
		/// <param name="uvw">Coordenadas barycentricas de colision</param>
		/// <param name="t">Instante de colision</param>
		/// <param name="col">Punto de colision</param>
		/// <returns>True si hay colision</returns>
		public static bool intersectSegmentTriangle(Vector3 p, Vector3 q, Vector3 a, Vector3 b, Vector3 c,
			out Vector3 uvw, out float t, out Vector3 col)
		{
			float u;
			float v;
			float w;
			uvw = Vector3.Zero;
			col = Vector3.Zero;
			t = -1;

			var ab = b - a;
			var ac = c - a;
			var qp = p - q;

			// Compute triangle normal. Can be precalculated or cached if
			// intersecting multiple segments against the same triangle
			var n = Vector3.Cross(ab, ac);

			// Compute denominator d. If d <= 0, segment is parallel to or points
			// away from triangle, so exit early
			var d = Vector3.Dot(qp, n);
			if (d <= 0.0f) return false;

			// Compute intersection t value of pq with plane of triangle. A ray
			// intersects iff 0 <= t. Segment intersects iff 0 <= t <= 1. Delay
			// dividing by d until intersection has been found to pierce triangle
			var ap = p - a;
			t = Vector3.Dot(ap, n);
			if (t < 0.0f) return false;
			if (t > d) return false; // For segment; exclude this code line for a ray test

			// Compute barycentric coordinate components and test if within bounds
			var e = Vector3.Cross(qp, ap);
			v = Vector3.Dot(ac, e);
			if (v < 0.0f || v > d) return false;
			w = -Vector3.Dot(ab, e);
			if (w < 0.0f || v + w > d) return false;

			// Segment/ray intersects triangle. Perform delayed division and
			// compute the last barycentric coordinate component
			var ood = 1.0f / d;
			t *= ood;
			v *= ood;
			w *= ood;
			u = 1.0f - v - w;

			uvw.X = u;
			uvw.Y = v;
			uvw.Z = w;
			col = p + t * (p - q);
			return true;
		}


		public static bool intersectRaySphere(Vector3 p0, Vector3 dir, Vector3 pos, float r, out float t, out Vector3 q)
		{
			t = -1;
			q = new Vector3();
			var m = p0 - pos;
			var b = Vector3.Dot(m, dir);
			var c = Vector3.Dot(m, m) - r*r;
			// Exit if r’s origin outside s (c > 0) and r pointing away from s (b > 0)
			if (c > 0.0f && b > 0.0f) return false;
			var discr = b * b - c;
			// A negative discriminant corresponds to ray missing sphere
			if (discr < 0.0f) return false;
			// Ray now found to intersect sphere, compute smallest t value of intersection
			t = -b - MathF.Sqrt(discr);
			// If t is negative, ray started inside sphere so clamp t to zero
			if (t < 0.0f) t = 0.0f;
			q = p0 + t * dir;
			return true;
		}


		/// <summary>
		///     Indica si un punto p en el espacio se encuentra dentro de un triangulo (a, b, c)
		/// </summary>
		/// <param name="p">Punto a probar</param>
		/// <param name="a">Vertice A del triangulo</param>
		/// <param name="b">Vertice B del triangulo</param>
		/// <param name="c">Vertice C del triangulo</param>
		/// <returns>True si el punto pertenece al triangulo</returns>
		public static bool testPointInTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
		{

			// verifico primero si vive en el mismo plano
			var n = Vector3.Cross(b - a, c - a);
			if (MathF.Abs(Vector3.Dot(p - a, n)) > 1.01f)
				return false;       // esta en otro plano


			// Translate point and triangle so that point lies at origin
			a -= p;
			b -= p;
			c -= p;

			// Compute normal vectors for triangles pab and pbc
			var u = Vector3.Cross(b, c);
			var v = Vector3.Cross(c, a);
			// Make sure they are both pointing in the same direction
			if (Vector3.Dot(u, v) < 0.0f) return false;
			// Compute normal vector for triangle pca
			var w = Vector3.Cross(a, b);
			// Make sure it points in the same direction as the first two
			if (Vector3.Dot(u, w) < 0.0f) return false;
			// Otherwise P must be in (or on) the triangle
			return true;
		}


	}
}
