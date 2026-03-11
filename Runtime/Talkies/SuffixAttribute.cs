using UnityEngine;


namespace SOSXR.Talkies
{
    public class SuffixAttribute : PropertyAttribute
    {
        public readonly string Suffix;


        public SuffixAttribute(string suffix)
        {
            Suffix = suffix;
        }
    }
}
