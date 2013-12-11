using System;
using System.Collections.Generic;

namespace SteveClientCore
{
	public class RequestCnt
	{
		public RequestCnt ()
		{
			name = "os";
			val = "linux";
		}

		public RequestCnt (string n, String v)
		{
			name = n;
			val = v;
		}

		public String name;
		public String val;
	}
}

