﻿using MemoryPack;
using MessagePack.Formatters;
using Microsoft.Diagnostics.Tracing.Parsers.ClrPrivate;
using Orleans.Serialization.Buffers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark.Micro;

public class GetLocalVsStaticField
{
    ArrayBufferWriter<byte> bufferWriter;

    public GetLocalVsStaticField()
    {
        bufferWriter = new ArrayBufferWriter<byte>();
        GetFromProvider();
    }

    [Benchmark(Baseline = true)]
    public void GetFromProvider()
    {
        var writer = new MemoryPackWriter<ArrayBufferWriter<byte>>(ref bufferWriter, MemoryPackSerializeOptions.Default);
        for (int i = 0; i < 100; i++)
        {
            writer.GetFormatter<int>().Serialize(ref writer, ref i);
        }
        bufferWriter.Clear();
    }

    [Benchmark]
    public void GetFromLocal()
    {
        var writer = new MemoryPackWriter<ArrayBufferWriter<byte>>(ref bufferWriter, MemoryPackSerializeOptions.Default);
        var provider = writer.GetFormatter<int>();
        for (int i = 0; i < 100; i++)
        {
            provider.Serialize(ref writer, ref i);
        }
        bufferWriter.Clear();
    }
}
