﻿using static OpenTaiko.BestPlayRecords;

namespace OpenTaiko {
	internal class CUnlockSG : CUnlockCondition {


		public CUnlockSG(CUnlockConditionFactory.UnlockConditionJsonRaw rawJson) : base(rawJson) {
			this.RequiredArgCount = 2;
			this.ConditionId = "sg";
		}

		public override (bool, string?) tConditionMet(int player, EScreen screen = EScreen.MyRoom) {
			if (this.Values.Length % this.RequiredArgCount == 0
						&& this.Reference.Length == this.Values.Length / this.RequiredArgCount) {
				int _satisfactoryPlays = this.tGetCountChartsPassingCondition(player);

				bool fulfiled = this.tValueRequirementMet(_satisfactoryPlays, this.Reference.Length);

				if (screen == EScreen.Internal) {
					return (fulfiled, "");
				} else {
					return (fulfiled, null);
				}
			} else
				return (false, CLangManager.LangInstance.GetString("UNLOCK_CONDITION_ERROR2", this.ConditionId, this.RequiredArgCount.ToString()));
		}

		public override string tConditionMessage(EScreen screen = EScreen.MyRoom) {
			if (!(this.Values.Length % this.RequiredArgCount == 0
						&& this.Reference.Length == this.Values.Length / this.RequiredArgCount))
				return CLangManager.LangInstance.GetString("UNLOCK_CONDITION_ERROR2", this.ConditionId, this.RequiredArgCount.ToString());

			// Only the player loaded as 1P can check unlockables in real time
			var SaveData = OpenTaiko.SaveFileInstances[OpenTaiko.SaveFile].data;
			var ChartStats = SaveData.bestPlaysStats;

			// Check distinct plays
			List<string> _rows = new List<string>();
			var _challengeCount = this.Values.Length / this.RequiredArgCount;

			var _count = 0;
			for (int i = 0; i < _challengeCount; i++) {
				int _base = i * this.RequiredArgCount;
				string _genreName = this.Reference[i];
				int _songCount = this.Values[_base];
				var _aimedStatus = this.Values[_base + 1];

				int _satifsiedCount = 0;
				if (_aimedStatus == (int)EClearStatus.NONE) _satifsiedCount = ChartStats.SongGenrePlays.TryGetValue(_genreName, out var value) ? value : 0;
				else if (_aimedStatus <= (int)EClearStatus.CLEAR) _satifsiedCount = ChartStats.SongGenreClears.TryGetValue(_genreName, out var value) ? value : 0;
				else if (_aimedStatus == (int)EClearStatus.FC) _satifsiedCount = ChartStats.SongGenreFCs.TryGetValue(_genreName, out var value) ? value : 0;
				else _satifsiedCount = ChartStats.SongGenrePerfects.TryGetValue(_genreName, out var value) ? value : 0;

				if (_satifsiedCount >= _songCount) _count++;


				var statusString = GetRequiredClearStatus(_aimedStatus);
				_rows.Add(CLangManager.LangInstance.GetString("UNLOCK_CONDITION_CHALLENGE_PLAYGENRE", statusString, _songCount, _genreName, _satifsiedCount));
			}

			_rows.Insert(0, CLangManager.LangInstance.GetString("UNLOCK_CONDITION_CHALLENGE", _count, _challengeCount));
			return String.Join("\n", _rows);
		}

		protected override int tGetCountChartsPassingCondition(int player) {
			var bpDistinctCharts = OpenTaiko.SaveFileInstances[player].data.bestPlaysDistinctCharts;
			var chartStats = OpenTaiko.SaveFileInstances[player].data.bestPlaysStats;

			var _count = 0;
			for (int i = 0; i < this.Values.Length / this.RequiredArgCount; i++) {
				int _base = i * this.RequiredArgCount;
				string _genreName = this.Reference[i];
				int _songCount = this.Values[_base];
				var _aimedStatus = this.Values[_base + 1];

				int _satifsiedCount = 0;
				if (_aimedStatus == (int)EClearStatus.NONE) _satifsiedCount = chartStats.SongGenrePlays.TryGetValue(_genreName, out var value) ? value : 0;
				else if (_aimedStatus <= (int)EClearStatus.CLEAR) _satifsiedCount = chartStats.SongGenreClears.TryGetValue(_genreName, out var value) ? value : 0;
				else if (_aimedStatus == (int)EClearStatus.FC) _satifsiedCount = chartStats.SongGenreFCs.TryGetValue(_genreName, out var value) ? value : 0;
				else _satifsiedCount = chartStats.SongGenrePerfects.TryGetValue(_genreName, out var value) ? value : 0;

				if (_satifsiedCount >= _songCount) _count++;
			}
			return _count;
		}
	}
}
