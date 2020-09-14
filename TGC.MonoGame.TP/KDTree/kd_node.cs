using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP
{
    public class kd_node
    {
		public Vector3 p_min;
		public Vector3 p_max;
		public int deep;
		public float split;
		public int split_plane;
		public kd_node p_left, p_right;
		public int cant_f;
		public int []p_list;

	}
}
