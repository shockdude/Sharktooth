using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;

namespace Sharktooth.Mub
{
    public class MubExport
    {
        private const int DELTA_TICKS_PER_QUARTER = 480;
        private Mub _mub;

        public MubExport(Mub mub)
        {
            _mub = mub;
        }

        public void Export(string path)
        {
            MidiEventCollection mid = new MidiEventCollection(1, DELTA_TICKS_PER_QUARTER);
            mid.AddTrack(CreateTempoTrack());
            mid.AddTrack(CreateTrack());
            List<MidiEvent> effectsTrack = CreateEffectsTrack();
            if (effectsTrack != null)
                mid.AddTrack(effectsTrack);

            MidiFile.Export(path, mid);
        }

        private List<MidiEvent> CreateTempoTrack()
        {
            List<MidiEvent> track = new List<MidiEvent>();

            // These are redundant but ehh
            track.Add(new NAudio.Midi.TextEvent("mubTempo", MetaEventType.SequenceTrackName, 0));
            track.Add(new TimeSignatureEvent(0, 4, 2, 24, 8)); // 4/4 ts
            track.Add(new TempoEvent(500000, 0)); // 120 bpm

            // Adds end track
            track.Add(new MetaEvent(MetaEventType.EndTrack, 0, track.Last().AbsoluteTime));
            return track;
        }

        private List<MidiEvent> CreateTrack()
        {
            List<MidiEvent> track = new List<MidiEvent>();
            track.Add(new NAudio.Midi.TextEvent("NOTES", MetaEventType.SequenceTrackName, 0));

            int ticksPerMeasure = DELTA_TICKS_PER_QUARTER * 4; // Assume 4/4
            foreach (var entry in _mub.Entries)
            {
                long start = (long)(entry.Start * ticksPerMeasure);
                long end = (long)(start + (entry.Length * ticksPerMeasure));

                // DJH2 effect type - 0x05FFFFFF through 0x06000009
                // Should not be added to the notes track
                if ((entry.Modifier & 0xFF000000) == 0x06000000 || entry.Modifier == 0x05FFFFFF) continue;

                if ((entry.Modifier & 0xFFFFFF) == 0xFFFFFF)
                {
                    // Text event?
                    if (!string.IsNullOrEmpty(entry.Text))
                        track.Add(new NAudio.Midi.TextEvent(entry.Text, MetaEventType.TextEvent, start));
                    continue;
                }

                if (entry.Length <= 0) continue;

                if (!string.IsNullOrEmpty(entry.Text))
                {
                    if ((entry.Modifier & 0x1000) == 0x1000)
                        track.Add(new NAudio.Midi.TextEvent(entry.Text, MetaEventType.Lyric, start)); // Lyric event?
                    else
                        track.Add(new NAudio.Midi.TextEvent(entry.Text, MetaEventType.TextEvent, start));
                }

                track.Add(new NoteEvent(start, 1, MidiCommandCode.NoteOn, entry.Modifier & 0xFF, 100));
                track.Add(new NoteEvent(end, 1, MidiCommandCode.NoteOff, entry.Modifier & 0xFF, 100));
            }

            track.Sort((x, y) =>
            {
                if (x.AbsoluteTime < y.AbsoluteTime)
                    return -1;
                else if (x.AbsoluteTime > y.AbsoluteTime)
                    return 1;
                else
                    return 0;
            });

            // Adds end track
            track.Add(new MetaEvent(MetaEventType.EndTrack, 0, track.Last().AbsoluteTime));
            return track;
        }

        private List<MidiEvent> CreateEffectsTrack()
        {
            List<MidiEvent> effects = null;

            int ticksPerMeasure = DELTA_TICKS_PER_QUARTER * 4; // Assume 4/4
            foreach (var entry in _mub.Entries)
            {
                long start = (long)(entry.Start * ticksPerMeasure);
                long end = (long)(start + (entry.Length * ticksPerMeasure));

                if ((entry.Modifier & 0xFF000000) == 0x06000000)
                {
                    if (effects == null)
                    {
                        effects = new List<MidiEvent>();
                        effects.Add(new NAudio.Midi.TextEvent("EFFECTS", MetaEventType.SequenceTrackName, 0));
                    }
                    effects.Add(new NoteEvent(start, 1, MidiCommandCode.NoteOn, entry.Modifier & 0xFF, 100));
                    effects.Add(new NoteEvent(end, 1, MidiCommandCode.NoteOff, entry.Modifier & 0xFF, 100));
                    continue;
                }
            }

            if (effects != null)
            {
                effects.Sort((x, y) =>
                {
                    if (x.AbsoluteTime < y.AbsoluteTime)
                        return -1;
                    else if (x.AbsoluteTime > y.AbsoluteTime)
                        return 1;
                    else
                        return 0;
                });

                // Adds end track
                effects.Add(new MetaEvent(MetaEventType.EndTrack, 0, effects.Last().AbsoluteTime));
            }
            return effects;
        }
    }
}
