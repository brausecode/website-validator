﻿using System.Collections.Generic;
using System.Linq;
using WebsiteValidator.BL.Interfaces;

namespace WebsiteValidator.BL.Classes
{
    public class UrlToAbsolutUrlConverter : IUrlToAbsolutUrlConverter
    {
        public string ToAbsoluteUrl(string baseUrl, string url)
        {
            var result = baseUrl;

            if (!result.EndsWith("/"))
            {
                result += "/";
            }

            if (url.StartsWith("/"))
            {
                var temp = url.Remove(0, 1); // remove "/"
                result += temp;
            }

            if (url.StartsWith("https://"))
            {
                return url;
            }

            return result;
        }

        public string[] ToAbsoluteUrl(string baseUrl, string[] links)
        {
            return links.Select(
                link => ToAbsoluteUrl(baseUrl, link)
                ).ToArray();
        }
    }
}