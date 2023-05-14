using System;

namespace Dman.LSystem.Extern
{
    [Serializable]
    public partial struct JaggedIndexing
    {
         public int Start => index;
         public int End => index + length;
    
         public static JaggedIndexing INVALID = new JaggedIndexing
         {
             index = -1,
             length = 0,
         };
    
         public static JaggedIndexing GetWithNoLength(int index)
         {
             return new JaggedIndexing
             {
                 index = index,
                 length = 0
             };
         }
         public static JaggedIndexing GetWithOnlyLength(ushort length)
         {
             return new JaggedIndexing
             {
                 index = -1,
                 length = length
             };
         }
    
         public bool ContainsIndex(int index)
         {
             return index >= this.Start && index < this.End;
         }
    
         public bool Equals(JaggedIndexing other)
         {
             return other.index == index && other.length == length;
         }
         public override bool Equals(object obj)
         {
             if (obj is JaggedIndexing indexing)
             {
                 return Equals(indexing);
             }
             return false;
         }
         public override int GetHashCode()
         {
             return index << 31 | length;
         }
    }
}