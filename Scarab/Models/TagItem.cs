using HarfBuzzSharp;

namespace Scarab.Models;

public class TagItem
{
    public string TagName { get; set; }
    public bool IsSelected { get; set; } = false;

    public TagItem(string tagName, bool isSelected)
    {
        TagName = tagName;
        IsSelected = isSelected;
    }
}