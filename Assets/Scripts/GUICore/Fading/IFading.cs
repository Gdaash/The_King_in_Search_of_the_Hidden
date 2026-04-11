using System;

namespace GUICore.Fading
{
    public interface IFading
    {
        public Action OnComplete { get; set; }

        public void In();
        public void Out();
    }
}