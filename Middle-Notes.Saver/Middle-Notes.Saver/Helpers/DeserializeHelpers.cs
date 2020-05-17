using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;

namespace Middle_Notes.Saver.Helpers
{
	public static class DeserializeHelpers
    {
        public static Dictionary<string, HttpStatusCode> GetErrorIdentifiers(string value)
        {
			try
			{
				return JsonConvert.DeserializeObject<Dictionary<string, HttpStatusCode>>(value);
			}
			catch (Exception)
			{
				return null;
			}
        }
    }
}
