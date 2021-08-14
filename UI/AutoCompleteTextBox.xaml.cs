using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace BitnuaVideoPlayer.UI
{
    /// <summary>
    /// Interaction logic for AutoCompleteTextBox.xaml
    /// </summary>
    public partial class AutoCompleteTextBox : UserControl
    {

        #region Default Constructor  

        /// <summary>  
        /// Initializes a new instance of the <see cref="AutoCompleteTextBoxUserControl" /> class.  
        /// </summary>  
        public AutoCompleteTextBox()
        {
            try
            {
                // Initialization.  
                InitializeComponent();
            }
            catch (Exception ex)
            {
                // Info.  
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.Write(ex);
            }
        }
        #endregion



        /// <summary>
        /// The collection to search for matches from.
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register
            (
                "ItemsSource",
                typeof(IEnumerable<string>),
                typeof(AutoCompleteTextBox),
                new UIPropertyMetadata(null)
            );
      
        /// <summary>
        /// What string should indicate that we should start giving auto-completion suggestions.  For example: @
        /// If this is null or empty, auto-completion suggestions will begin at the beginning of the textbox's text.
        /// </summary>
        public static readonly DependencyProperty IndicatorProperty =
            DependencyProperty.Register
            (
                "Indicator",
                typeof(string),
                typeof(AutoCompleteTextBox),
                new UIPropertyMetadata(string.Empty)
            );

        public static readonly DependencyProperty AutoCompleteTextProperty =
           DependencyProperty.Register
           (
               "AutoCompleteText",
               typeof(string),
               typeof(AutoCompleteTextBox),
               new FrameworkPropertyMetadata("")
           );

        private Key m_lastKey;

        public IEnumerable<string> ItemsSource
        {
            get
            {
                object objRtn = GetValue(ItemsSourceProperty);
                if (objRtn is IEnumerable<string>)
                    return (objRtn as IEnumerable<string>);

                return null;
            }
            set
            {
                SetValue(ItemsSourceProperty, value);
            }
        }

        public string Indicator
        {
            get => (string)GetValue(IndicatorProperty);
            set => SetValue(IndicatorProperty, value);

        }

        public string AutoCompleteText
        {
            get => (string)GetValue(AutoCompleteTextProperty);
            set => SetValue(AutoCompleteTextProperty, value);

        }

        /// <summary>
        /// Used for moving the caret to the end of the suggested auto-completion text.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void onKeyDown(object sender, KeyEventArgs e)
        {
            var tb = autoTextBox;

            if (e.Key == Key.Enter)
            {
                var a = 2;
                a += 1;
            }
            //If we pressed enter and if the selected text goes all the way to the end, move our caret position to the end
            if (e.Key == Key.Enter && tb.SelectionLength > 0 && (tb.SelectionStart + tb.SelectionLength == tb.Text.Length))
            {
                tb.SelectionStart = tb.CaretIndex = tb.Text.Length;
                tb.SelectionLength = 0;
                CloseAutoSuggestionBox();
            }
            else if (e.Key == Key.Down || e.Key == Key.Up)
            {
                if (autoList.Items.Count > 1)
                {
                    autoList.SelectedIndex = Math.Abs((autoList.SelectedIndex + (e.Key == Key.Down ? 1 : -1)) % autoList.Items.Count);
                }
            }

            m_lastKey = e.Key;
        }

        /// <summary>
        /// Search for auto-completion suggestions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void onTextChanged(object sender, TextChangedEventArgs e)
        {

            updateBindingSource();
            
            // Verification.  
            if (m_lastKey == default(Key) || string.IsNullOrEmpty(this.autoTextBox.Text))
            {
                // Disable.  
                this.CloseAutoSuggestionBox();

                // Info.  
                return;
            }


            TextBox tb = autoTextBox;

            IEnumerable<string> values = ItemsSource;
            //No reason to search if we don't have any values.
            if (values == null)
                return;

            //No reason to search if there's nothing there.
            if (string.IsNullOrEmpty(tb.Text))
                return;


            string matchingstring;
            findMatch(out _, out matchingstring);


            //If we don't have anything after the trigger string, return.
            if (string.IsNullOrEmpty(matchingstring))
            {
                this.autoList.ItemsSource = values;
                this.autoList.SelectedIndex = -1;
                this.OpenAutoSuggestionBox();
                return;
            }


            // Settings.  
            var matches = values.Where(p => p.ToLower().StartsWith(matchingstring.ToLower())).ToList();
            this.autoList.ItemsSource = matches;

            if (matches.Count > 0)
            {
                this.OpenAutoSuggestionBox();
                if (!(m_lastKey == Key.Back || m_lastKey == Key.Delete))
                    this.autoList.SelectedIndex = -1;
                    this.autoList.SelectedIndex = 0;
            }
        }

        private void findMatch(out int startIndex, out string matchingstring)
        {
            var tb = autoTextBox;
            string indicator = Indicator;
            startIndex = 0;
            matchingstring = tb.Text;
            //If we have a trigger string, make sure that it has been typed before
            //giving auto-completion suggestions.
            if (!string.IsNullOrEmpty(indicator))
            {
                startIndex = tb.Text.LastIndexOf(indicator);
                //If we haven't typed the trigger string, then don't do anything.
                if (startIndex == -1)
                    return;

                startIndex += indicator.Length;
                matchingstring = tb.Text.Substring(startIndex, (tb.Text.Length - startIndex));
            }
        }



        #region Open Auto Suggestion box method  

        /// <summary>  
        ///  Open Auto Suggestion box method  
        /// </summary>  
        private void OpenAutoSuggestionBox()
        {
            try
            {
                // Enable.  
                this.autoListPopup.Visibility = Visibility.Visible;
                this.autoListPopup.IsOpen = true;
                this.autoList.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                // Info.  
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.Write(ex);
            }
        }

        #endregion

        #region Close Auto Suggestion box method  

        /// <summary>  
        ///  Close Auto Suggestion box method  
        /// </summary>  
        private void CloseAutoSuggestionBox()
        {
            try
            {
                // Enable.  
                this.autoListPopup.Visibility = Visibility.Collapsed;
                this.autoListPopup.IsOpen = false;
                this.autoList.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                // Info.  
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.Write(ex);
            }
        }

        #endregion

        #region Auto list selection changed method  

        /// <summary>  
        ///  Auto list selection changed method.  
        /// </summary>  
        /// <param name="sender">Sender parameter</param>  
        /// <param name="e">Event parameter</param>  
        private void AutoList_SelectionChanged(object sender, SelectionChangedEventArgs e) => _SelectionChanged();
        private void _SelectionChanged()
        {
            // Verification.  
            if (this.autoList.SelectedIndex <= -1)
            {
                // Info.  
                return;
            }


            var tb = autoTextBox;

            var match = this.autoList.SelectedItem.ToString();
            findMatch(out int startIndex, out string matchingstring);
            if (startIndex == -1)
                return;

            int textLength = matchingstring.Length;



            int matchStart = (startIndex + matchingstring.Length);
            tb.TextChanged -= onTextChanged;

            var text = $"{tb.Text.Substring(0, startIndex)}{match}";

            tb.SetCurrentValue(TextBox.TextProperty, text);

            updateBindingSource();

            if (matchStart > tb.Text.Length)
                matchStart = startIndex;

            tb.CaretIndex = matchStart;
            tb.SelectionStart = matchStart;
            tb.SelectionLength = (tb.Text.Length - matchStart);
            tb.TextChanged += onTextChanged;
        }

        private void updateBindingSource()
        {
            autoTextBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
            GetBindingExpression(AutoCompleteTextProperty).UpdateSource();
        }

        #endregion
    }
}