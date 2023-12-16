using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.ViewModel;

// ReSharper disable MemberCanBePrivate.Global

namespace IctBaden.Stonehenge.Vue.SampleCore.ViewModels
{
    public class ImagesVm : ActiveViewModel
    {
        public bool IsOn { get; set; }

        public string SwitchImg => IsOn 
            ? "images/switch_on.png" 
            : "images/switch_off.png";

        public string LampImg => IsOn 
            ? "images/LightBulb_on.png" 
            : "images/LightBulb.png";

        public ImagesVm(AppSession session)
            : base(session)
        {
        }

        [ActionMethod]
        public void Switch()
        {
            IsOn = !IsOn;
            
            ExecuteClientScript(IsOn 
                ? "document.getElementById('on').play()" 
                : "document.getElementById('off').play()");
        }
        
    }
}