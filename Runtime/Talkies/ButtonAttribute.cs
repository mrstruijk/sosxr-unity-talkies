using System;


namespace SOSXR.Talkies
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ButtonAttribute : Attribute
    {
        public ButtonAttribute(string itemName = null, string tooltip = null, int space = 0, bool horizontalLine = false)
        {
            ItemName = itemName;
            Tooltip = tooltip;
            Space = space;
            HorizontalLine = horizontalLine;
        }


        public string Tooltip { get; set; }
        public string ItemName { get; set; }
        public int Space { get; set; }
        public bool HorizontalLine { get; set; }
    }
}
