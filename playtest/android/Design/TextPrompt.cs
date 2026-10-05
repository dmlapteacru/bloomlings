using System;
using Android.App;
using Android.Content;
using Android.Text;
using Android.Views;
using Android.Widget;
using Bloomlings.Playtest.Design;

namespace Bloomlings.Playtest.Droid
{
    /// <summary>
    /// The full playtest's text entry (the profile's name, spec 005 FR-037): the system's dialog with one text field and
    /// the keyboard up, OK and Cancel. OK hands the typed text to the app on the UI thread and redraws the view; Cancel
    /// and a tap outside leave everything as it was.
    /// </summary>
    public sealed class TextPrompt : ITextPrompt
    {
        private readonly Context _context;
        private readonly View _view;

        public TextPrompt(Context context, View view)
        {
            _context = context;
            _view = view;
        }

        public void Ask(string title, string text, int maxLength, Action<string> done)
        {
            var field = new EditText(_context)
            {
                Text = text,
                InputType = InputTypes.ClassText | InputTypes.TextFlagCapWords,
            };
            field.SetSingleLine(true);
            field.SetFilters(new IInputFilter[] { new InputFilterLengthFilter(maxLength) });
            field.SetSelectAllOnFocus(true);
            var frame = new FrameLayout(_context);
            int pad = (int)(20 * _context.Resources!.DisplayMetrics!.Density);
            frame.SetPadding(pad, pad / 2, pad, 0);
            frame.AddView(field);

            AlertDialog dialog = new AlertDialog.Builder(_context)
                .SetTitle(title)!
                .SetView(frame)!
                .SetPositiveButton(PlaytestText.T("common.ok"), (sender, args) =>
                {
                    done(field.Text ?? string.Empty);
                    _view.Invalidate();
                })!
                .SetNegativeButton(PlaytestText.T("common.cancel"), (sender, args) => _view.Invalidate())!
                .Create()!;
            dialog.Window?.SetSoftInputMode(SoftInput.StateAlwaysVisible);
            dialog.Show();
            field.RequestFocus();
        }
    }
}
