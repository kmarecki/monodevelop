// 
// CairoUtil.cs
//  
// Author: Krzysztof Marecki
// 
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using Cairo;

namespace Stetic
{
	public class CairoUtil
	{	

		public static Color ColorFromHexa (String str, double alpha)
		{
			Gdk.Color c = new Gdk.Color ();
			Gdk.Color.Parse (str, ref c);
			return ColorFromRgb (c.Red / 255, c.Green / 255, c.Blue / 255, alpha);
		}
		
		public static Color ColorFromHexa (String str)
		{
			return ColorFromHexa (str, 1);
		}
		
		public static Color ColorFromRgb (int red, int green, int blue)
		{
			return ColorFromRgb (red, green, blue, 1);
		}
		
		public static Color ColorFromRgb (int red, int green, int blue, double alpha)
		{
			double r = Math.Round ((double)red / 255, 2);
			double g = Math.Round ((double)green / 255, 2);
			double b = Math.Round ((double)blue / 255, 2);
			
			return new Color (r, g, b, alpha);
		}
	}
}

