using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Reflection.Metadata.Ecma335;

namespace TGC.MonoGame.TP
{
	// carga la textura a mano, para no tener que meterla en el Content, ademas genera los mipmaps
	// si la textura es multiplo de pot de 2
	public class CTextureLoader
	{
		public static bool ispot2(int n)
		{
			return (n != 0) && ((n & (n - 1)) == 0);
		}


		public static Texture2D Load(GraphicsDevice device, string name)
		{
			if (!File.Exists(name))
				return null;


			Texture2D texture = null;
			FileStream fileStream = new FileStream(name, FileMode.Open);
			var tx = Texture2D.FromStream(device, fileStream);
			fileStream.Dispose();
			int W = tx.Width;
			int H = tx.Height;
			var dataColors = new Color[W * H];
			tx.GetData(dataColors);

			// nivel cero
			int level = 0;
			bool mipmap = ispot2(W) && ispot2(H) && W == H;
			texture = new Texture2D(device, W, H, mipmap, tx.Format);
			texture.SetData(level, new Rectangle(0, 0, W, H), dataColors, 0, W * H);
			// downsample mipmap
			if (mipmap)
				while (W > 1 || H > 1)
				{
					W /= 2;
					if (W < 1) W = 1;
					H /= 2;
					if (H < 1) H = 1;
					level++;
					var dataColorsM = new Color[W * H];

					for (int x = 0; x < W; ++x)
					{
						for (int y = 0; y < H; ++y)
						{
							int xa = 2 * x;
							int ya = 2 * y;
							int xb = xa + 1;
							int yb = ya + 1;
							Color s1 = dataColors[xa + ya * 2 * W];
							Color s2 = dataColors[xb + ya * 2 * W];
							Color s3 = dataColors[xb + yb * 2 * W];
							Color s4 = dataColors[xb + yb * 2 * W];
							dataColorsM[x + y * W].R = (byte)((s1.R + s2.R + s3.R + s4.R) * 0.25f);
							dataColorsM[x + y * W].G = (byte)((s1.G + s2.G + s3.G + s4.G) * 0.25f);
							dataColorsM[x + y * W].B = (byte)((s1.B + s2.B + s3.B + s4.B) * 0.25f);
							dataColorsM[x + y * W].A = (byte)((s1.A + s2.A + s3.A + s4.A) * 0.25f);
						}
					}
					texture.SetData(level, new Rectangle(0, 0, W, H), dataColorsM, 0, W * H);
					dataColors = dataColorsM;
				}

			return texture;
		}
		
	}
}
