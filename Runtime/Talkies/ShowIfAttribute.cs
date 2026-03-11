using System;
using UnityEngine;


namespace SOSXR.Talkies
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ShowIfAttribute : PropertyAttribute
    {
        public readonly string ConditionField;


        public ShowIfAttribute(string conditionField)
        {
            ConditionField = conditionField;
        }
    }
}
