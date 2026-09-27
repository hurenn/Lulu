// ボス「霧のゼルクス」専用のボス戦イベント
public class StageEvent_ZelxMistBattle : StageEvent_BossBattle {
    protected override void _OnBossDowned() {
        _currentMessageTriggerDelay = 0f;
        _messageViewer?.ForceReset();
        _isHalfHp = true;
        // Zelxは通常のダウン演出(_BossDownedRoutine、プレイヤー操作停止+スパークエフェクト)を使わないためカットする
    }
}
