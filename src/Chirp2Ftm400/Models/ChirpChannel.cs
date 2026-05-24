namespace Chirp2Ftm400.Models;

public sealed class ChirpChannel
{
    public int Location { get; set; }
    public string? Name { get; set; }
    public decimal Frequency { get; set; }
    public string? Duplex { get; set; }
    public decimal Offset { get; set; }
    public string? Tone { get; set; }
    public string? rToneFreq { get; set; }
    public string? cToneFreq { get; set; }
    public string? DtcsCode { get; set; }
    public string? DtcsPolarity { get; set; }
    public string? RxDtcsCode { get; set; }
    public string? CrossMode { get; set; }
    public string? Mode { get; set; }
    public string? TStep { get; set; }
    public string? Skip { get; set; }
    public string? Power { get; set; }
    public string? Comment { get; set; }
    public string? URCALL { get; set; }
    public string? RPT1CALL { get; set; }
    public string? RPT2CALL { get; set; }
    public string? DVCODE { get; set; }
}
