using MGAO.Core.Interfaces;

namespace MGAO.UI;

public class CalendarSelectionForm : Form
{
    private readonly CheckedListBox _calendarList;
    private readonly Button _okButton;
    private readonly Button _cancelButton;

    public List<CalendarInfo> SelectedCalendars { get; } = new();

    public CalendarSelectionForm(IEnumerable<CalendarInfo> calendars)
    {
        Text = "Select Calendars to Sync";
        Size = new Size(400, 350);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var label = new Label
        {
            Text = "Select the calendars you want to sync with Outlook:",
            Location = new Point(12, 12),
            AutoSize = true
        };

        _calendarList = new CheckedListBox
        {
            Location = new Point(12, 40),
            Size = new Size(360, 220),
            CheckOnClick = true
        };

        foreach (var cal in calendars)
        {
            _calendarList.Items.Add(cal, true);
        }

        _okButton = new Button
        {
            Text = "OK",
            Location = new Point(216, 270),
            Size = new Size(75, 30),
            DialogResult = DialogResult.OK
        };
        _okButton.Click += OnOk;

        _cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(297, 270),
            Size = new Size(75, 30),
            DialogResult = DialogResult.Cancel
        };

        Controls.AddRange(new Control[] { label, _calendarList, _okButton, _cancelButton });
        AcceptButton = _okButton;
        CancelButton = _cancelButton;
    }

    private void OnOk(object? sender, EventArgs e)
    {
        SelectedCalendars.Clear();
        foreach (var item in _calendarList.CheckedItems)
        {
            if (item is CalendarInfo cal)
            {
                SelectedCalendars.Add(cal);
            }
        }
    }
}
