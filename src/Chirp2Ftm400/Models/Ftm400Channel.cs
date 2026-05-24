namespace Chirp2Ftm400.Models;

public sealed class Ftm400Channel
{
    public int Channel { get; set; }
    public string RxFreq { get; set; } = "";
    public string TxFreq { get; set; } = "";
    public string Offset { get; set; } = "";
    public string Direction { get; set; } = "";
    public string Mode { get; set; } = "";
    public string Name { get; set; } = "";
    public string ToneMode { get; set; } = "";
    public string CTCSS { get; set; } = "";
    public string DCS { get; set; } = "";
    public string UserCTCSS { get; set; } = "";
    public string Power { get; set; } = "";
    public string Skip { get; set; } = "";
    public string Step { get; set; } = "";
    public int MemoryTag { get; set; } = 0;
    public string Comment { get; set; } = "";
    public int MemoryTag2 { get; set; } = 0;
}
