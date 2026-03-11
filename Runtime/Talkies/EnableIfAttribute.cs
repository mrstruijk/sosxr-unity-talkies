using System;
using UnityEngine;


namespace SOSXR.Talkies
{
    [AttributeUsage(AttributeTargets.Field)]
    public class EnableIfAttribute : PropertyAttribute
    {
        public readonly string ConditionField;


        public EnableIfAttribute(string conditionField)
        {
            ConditionField = conditionField;
        }
    }
}
