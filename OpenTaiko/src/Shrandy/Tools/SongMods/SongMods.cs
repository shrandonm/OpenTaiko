namespace OpenTaiko.Shrandy.Tools
{
	internal class SongMods
	{
		internal bool RemoveEvenNotes { get; private set; }
		internal bool RemoveOddNotes { get; private set; }

		private SongModsUI m_UI;

		internal SongMods()
		{
			m_UI = new SongModsUI(this);
		}

		internal void Draw()
		{
			m_UI.Draw();
		}

		internal void SetRemoveEvenNotes(bool value)
		{
			RemoveEvenNotes = value;
			ApplyNoteMods();
		}

		internal void SetRemoveOddNotes(bool value)
		{
			RemoveOddNotes = value;
			ApplyNoteMods();
		}

		private void ApplyNoteMods()
		{
			var listChip = OpenTaiko.TJA?.listChip;
			if (listChip == null) return;

			int noteIndex = 0;
			foreach (var chip in listChip)
			{
				if (NotesManager.IsMissableNote(chip))
				{
					noteIndex++;
					bool isEven = noteIndex % 2 == 0;
					bool shouldRemove = (isEven && RemoveEvenNotes) || (!isEven && RemoveOddNotes);
					chip.bVisible = !shouldRemove;
					chip.bShow = !shouldRemove;
				}
			}
		}
	}
}
