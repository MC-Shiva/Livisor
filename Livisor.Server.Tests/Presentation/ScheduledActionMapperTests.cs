using Livisor.Server.Domain;
using Livisor.Server.Domain.ValueObject;
using Livisor.Server.Presentation.Mapping;
using Livisor.Shared.Common;
using Livisor.Shared.DTO;

namespace Livisor.Server.Tests.Presentation;

// DTO ↔ Domain 変換（予約アクション）の検証。
// Docs/Rules/test.md は Domain / Application / Infrastructure の 3 層を規定するが、
// 通信境界(Presentation)の変換はどの既存層にも当てはまらないため、ここに置く。
public class ScheduledActionMapperTests
{
    [Fact]
    public void ToDomain_ValidAction_ReturnsScheduledAction()
    {
        var dto = new TimelineAction { Time = "00:00:05:00", Action = ActionType.VolumeChange, Value = 10 };

        var action = ScheduledActionMapper.ToDomain(dto);

        Assert.Equal("00:00:05:00", action.Offset.ToRawString());
        Assert.Equal(ActionType.VolumeChange, action.Action);
        Assert.Equal(10, action.Value.Number);
    }

    [Fact]
    public void ToDomain_Null_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => ScheduledActionMapper.ToDomain(null!));
    }

    [Fact]
    public void ToDomain_InvalidTime_ThrowsDomainException()
    {
        var dto = new TimelineAction { Time = "10:00:00", Action = ActionType.Play, Value = true };

        Assert.Throws<DomainException>(() => ScheduledActionMapper.ToDomain(dto));
    }

    [Fact]
    public void ToDto_Roundtrip_PreservesValues()
    {
        var dto = new TimelineAction { Time = "01:02:03:04", Action = ActionType.Play, Value = false };

        var restored = ScheduledActionMapper.ToDto(ScheduledActionMapper.ToDomain(dto));

        Assert.Equal("01:02:03:04", restored.Time);
        Assert.Equal(ActionType.Play, restored.Action);
        Assert.False(restored.Value.Bool);
    }
}
