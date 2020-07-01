using UnityEngine;

namespace Lunra.Core
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
			return NewRgba(color, r?.r ?? color.r, g?.g ?? color.g, b?.b ?? color.b, a?.a ?? color.a);
		}

		public static Color NewRgba(this Color color, float? r = null, float? g = null, float? b = null, float? a = null)
        {	
        	return new Color(r ?? color.r, g ?? color.g, b ?? color.b, a ?? color.a);
		}
		#endregion

		#region Hsv
		public static float GetH(this Color color)
		{
			Color.RGBToHSV(color, out var h, out _, out _);
			return h;
		}

		public static float GetS(this Color color)
		{
			Color.RGBToHSV(color, out _, out var s, out _);
			return s;
		}

		public static float GetV(this Color color)
		{
			Color.RGBToHSV(color, out _, out _, out var v);
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
	        Color.RGBToHSV(h ?? color, out var hH, out _, out _);

	        Color.RGBToHSV(s ?? color, out _, out var sS, out _);

	        Color.RGBToHSV(v ?? color, out _, out _, out var vV);

			return Color.HSVToRGB(hH, sS, vV, hdr).NewA(a?.a ?? color.a);
        }

		public static Color NewHsva(this Color color, float? h = null, float? s = null, float? v = null, float? a = null, bool hdr = false)
        {
	        Color.RGBToHSV(color, out var wasH, out var wasS, out var wasV);
			return Color.HSVToRGB(h ?? wasH, s ?? wasS, v ?? wasV, hdr).NewA(a ?? color.a);
		}
		#endregion

		#region Shared
		public static Color NewA(this Color color, float a)
		{	
			return new Color(color.r, color.g, color.b, a);
		}
		#endregion

		#region Extended Utility
		public static bool Approximately(this Color color, Color other)
		{
			return
					Mathf.Approximately(color.r, other.r) &&
					Mathf.Approximately(color.g, other.g) &&
					Mathf.Approximately(color.b, other.b) &&
					Mathf.Approximately(color.a, other.a);
		}

		public static string ToHtmlRgb(this Color color) => ColorUtility.ToHtmlStringRGB(color);
		public static string ToHtmlRgba(this Color color) => ColorUtility.ToHtmlStringRGBA(color);
		#endregion
	}
}