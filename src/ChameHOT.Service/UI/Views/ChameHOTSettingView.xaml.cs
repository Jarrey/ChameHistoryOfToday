using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.System.UserProfile;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using NoteOne_ImageHelper;
using Windows.UI.Xaml.Media.Imaging;
using ChameHOT_Service.UI.ViewModels;
using NoteOne_Utility.Extensions;
using NoteOne_Utility;
using System.Threading.Tasks;
using NoteOne_Core.Common;
using NoteOne_Core.Command;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace ChameHOT_Service.UI.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ChameHOTSettingView : NoteOne_Core.UI.Common.LayoutAwarePage
    {
        public ChameHOTSettingView()
        {
            this.InitializeComponent();

            // initial value for UI elements
            this.DefaultViewModel = new InternalViewModel(this);
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected async override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            // load from bootstrapper
            if (navigationParameter != null)
            {
                var initializeData = navigationParameter.GetType().GetTypeInfo().GetMemberInfo("InitializeData", MemberType.Method) as MethodInfo;
                if (initializeData != null)
                {
                    await (IAsyncAction)initializeData.Invoke(navigationParameter, null);
                    await InitCurrentViewSettings();
                }
            }

            this.DefaultViewModel = new ChameHOTSettingViewModel(this, pageState);

            // If main page init count can mod 10, show rate and review prompt
            await ChameHOTServiceHelper.CheckAndShowRateReviewPromptAsync();
        }

        private async Task InitCurrentViewSettings()
        {
            ChameHOTServiceSetting.Instance.Settings[ChameHOTServiceSetting.SCREEN_WIDTH] = Window.Current.Bounds.Width;
            ChameHOTServiceSetting.Instance.Settings[ChameHOTServiceSetting.SCREEN_HEIGHT] = Window.Current.Bounds.Height;

            await NoteOne_Utility.AppSettings.SaveSettings(ChameHOTServiceSetting.Instance);
        }

        internal class InternalViewModel : ViewModelBase
        {
            public InternalViewModel(FrameworkElement view)
                : base(view, null)
            {
                this["ShowInLeft"] = true;
                this["Settings"] = new { POSITION_LEFT = 800, POSITION_TOP = 100 };
                this["PointerMovedCommand"] = new RelayCommand(() => { });
            }
            public override void LoadState()
            {
            }

            public override void SaveState(Dictionary<string, object> pageState)
            {
            }
        }
    }
}
