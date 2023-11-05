using Beatmap.Enums;

namespace ChroMapper_LightModding.Models
{
    public class SelectedObject
    {
        public float Beat { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }

        public ObjectType ObjectType { get; set; }
        public int Color { get; set; }

        public override string ToString()
        {
            return $"{Beat}";
        }

        public string ToStringFull()
        {
            return $"{Beat} ({PosX},{PosY}) {Color} {ObjectType}";
        }
    }
}
