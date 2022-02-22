namespace MACsharp.Structs
{
    using Enums;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class Property
    {
        public static PropertyKind None => PropertyKind.None;
        public static PropertyKind Movable => PropertyKind.Movable;
        public static PropertyKind Cuttable => PropertyKind.Cuttable;
        public static PropertyKind IsCut => PropertyKind.IsCut;
        public static PropertyKind IsDeliverable => PropertyKind.Deliverable;
        public static PropertyKind IsDelivered => PropertyKind.IsDelivered;
        
        public static List<PropertyKind> Values = Enum.GetValues(typeof(PropertyKind)).Cast<PropertyKind>().Where(p => p != None).ToList();

        public static IEnumerable<PropertyKind> GetFlags(this PropertyKind input)
        {
            return Values.Where(value => input.HasFlag(value));
        }

        public static int GetSetBitCount(PropertyKind input)
        {
            long lValue = (long)input;
            int iCount = 0;

            //Loop the value while there are still bits
            while (lValue != 0)
            {
                //Remove the end bit
                lValue = lValue & (lValue - 1);

                //Increment the count
                iCount++;
            }

            //Return the count
            return iCount;
        }
    }
}
