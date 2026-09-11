using Livisor.Server.Presentation.Mapping;
using Livisor.Shared.Common;
using Livisor.Shared.DTO;

namespace Livisor.Server.Tests.Presentation;

// Shared に固定で持つデフォルト演出（Issue #22）の検証。
// 定義は C# なので型は合うが、時刻の書式・演出名の妥当性はここで固定する（並び順は問わない。クライアントが並べ替える）。
// サーバーは起動時に同じ検証（ScheduledActionMapper.ToDomain）を通す。
public class DefaultActionSetTests
{
    [Fact]
    public void Create_Always_PassesScheduledActionValidation()
    {
        foreach (var action in DefaultActionSet.Create())
            ScheduledActionMapper.ToDomain(action); // 不正なら DomainException
    }

    [Fact]
    public void Create_EffectActions_UseKnownEffectNames()
    {
        // 知らない名前はクライアントが無視するため、定義側で既知の名前に限る。
        var known = new[] { EffectNames.ConfettiOn, EffectNames.ConfettiOff, EffectNames.Lightning, EffectNames.SilverStreamer };

        foreach (var action in DefaultActionSet.Create().Where(a => a.Action == ActionType.Effect))
            Assert.Contains(action.Value.Text, known);

        foreach (var name in known)
            Assert.Contains(DefaultActionSet.Create(), a => a.Action == ActionType.Effect && a.Value.Text == name);
    }

    [Fact]
    public void Create_CalledTwice_ReturnsNewInstances()
    {
        var first = DefaultActionSet.Create();
        var second = DefaultActionSet.Create();
        var expected = second.Select(a => (a.Time, a.Action, a.Value)).ToArray();
        foreach (var action in first)
            action.Value = "modified";

        Assert.NotSame(first, second);
        Assert.Equal(expected, second.Select(a => (a.Time, a.Action, a.Value)));
    }
}
