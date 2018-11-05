using System;
using Autodesk.Connectivity.WebServices;

namespace SD226856.RdlcHelper
{
    public static class ExtensionMethods
    {
        public static Type ToDotNetType(this DataType dataType)
        {
            switch (dataType)
            {
                case DataType.String: { return typeof(string); }
                case DataType.Numeric: { return typeof(double); }
                case DataType.Bool: { return typeof(byte); }
                case DataType.DateTime: { return typeof(DateTime); }
                case DataType.Image: { return typeof(string); }
                default:
                {
                    throw new ArgumentException(
                        $"Type '{dataType.ToString()}' cannot be assigned to a .NET type");
                }
            }
        }

        public static string ToDataColumnCaption(this string columnName)
        {
            var pattern = "[^A-Za-z0-9]";
            return System.Text.RegularExpressions.Regex.Replace(columnName, pattern, "_");
        }
    }
}
