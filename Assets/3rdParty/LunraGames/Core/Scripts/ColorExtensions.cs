using UnityEngine;

namespace LunraGames
{
	public static class ColorExtensions
	{
		#region Rgb
		public static Color NewR(this Color color, float r)
        {	
        	return new Color(r, color.g, color.b, color.a);
		}

		public static Color NewG(this Color color, float g)
        {	
			return new Color(color.r, g, color.b, color.a);
		}

		public static Color NewB(this Color color, float b)
        {	
			return new Color(color.r, color.g, b, color.a);
        }

		public static Color NewR(this Color color, Color r)
        {	
        	return NewRgba(color, r: r.r);
		}

		public static Color NewG(this Color color, Color g)
        {	
			return NewRgba(color, g: g.g);
		}

		public static Color NewB(this Color color, Color b)
        {	
			return NewRgba(color, b: b.b);
        }

		public static Color NewRgba(this Color color, Color? r = null, Color? g = null, Color? b = null, Color? a = null)
        {	
			return NewRgba(color, r.HasValue ? r.Value.r : color.r, g.HasValue ? g.Value.g : color.g, b.HasValue ? b.Value.b : color.b, a.HasValue ? a.Value.a : color.a);
		}

		public static Color NewRgba(this Color color, float? r = null, float? g = null, float? b = null, float? a = null)
        {	
        	return new Color(r ?? color.r, g ?? color.g, b ?? color.b, a ?? color.a);
		}
		#endregion

		#region Hsv
		public static float GetH(this Color color)
		{
			float h, s, v;
			Color.RGBToHSV(color, out h, out s, out v);
			return h;
		}

		public static float GetS(this Color color)
		{
			float h, s, v;
			Color.RGBToHSV(color, out h, out s, out v);
			return s;
		}

		public static float GetV(this Color color)
		{
			float h, s, v;
			Color.RGBToHSV(color, out h, out s, out v);
			return v;
		}

		public static Color NewH(this Color color, float h)
        {	
        	return NewHsva(color, h);
		}

		public static Color NewS(this Color color, float s)
        {	
			return NewHsva(color, s: s);
		}

		public static Color NewV(this Color color, float v)
        {	
			return NewHsva(color, v: v);
        }

		public static Color NewH(this Color color, Color h)
        {	
        	return NewHsva(color, h);
		}

		public static Color NewS(this Color color, Color s)
        {	
			return NewHsva(color, s: s);
		}

		public static Color NewV(this Color color, Color v)
        {	
			return NewHsva(color, v: v);
        }

		public static Color NewHsva(this Color color, Color? h = null, Color? s = null, Color? v = null, Color? a = null, bool hdr = false)
        {	
			float hH, hS, hV;
			Color.RGBToHSV(h.HasValue ? h.Value : color, out hH, out hS, out hV);

			float sH, sS, sV;
			Color.RGBToHSV(s.HasValue ? s.Value : color, out sH, out sS, out sV);

			float vH, vS, vV;
			Color.RGBToHSV(v.HasValue ? v.Value : color, out vH, out vS, out vV);

			return Color.HSVToRGB(hH, sS, vV, hdr).NewA(a.HasValue ? a.Value.a : color.a);
        }

		public static Color NewHsva(this Color color, float? h = null, float? s = null, float? v = null, float? a = null, bool hdr = false)
        {	
        	float wasH;
			float wasS;
			float wasV;
			Color.RGBToHSV(color, out wasH, out wasS, out wasV);
			return Color.HSVToRGB(h ?? wasH, s ?? wasS, v ?? wasV, hdr).NewA(a ?? color.a);
		}
		#endregion

		#region Shared
		public static Color NewA(this Color color, float a)
		{	
			return new Color(color.r, color.g, color.b, a);
		}
		#endregion

		public static bool Approximately(this Color color, Color other)
		{
			return
					Mathf.Approximately(color.r, other.r) &&
					Mathf.Approximately(color.g, other.g) &&
					Mathf.Approximately(color.b, other.b) &&
					Mathf.Approximately(color.a, other.a);
		}
	}
}