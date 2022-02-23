using ProtoBuf;

using System;
using System.Collections.Generic;
using System.Drawing;

namespace SharedLibrary
{
    [ProtoContract]
    public class Payload
    {
        [ProtoMember(1)]
        public string Command = "";

        [ProtoMember(2)]
        public int Port;

        // Window properties

        [ProtoMember(3)]
        public int WindowHandle;

        [ProtoMember(4)]
        public string WindowTitle = "";

        [ProtoMember(5)]
        public List<byte[]> WindowImagesInBytes;

        // NOT SERIALIZED
        public Bitmap[,] WindowImages;

        [ProtoMember(6)]
        public int WindowHeight;

        [ProtoMember(7)]
        public int WindowWidth;

        [ProtoMember(8)]
        public bool WindowIsFocused = false;

        [ProtoMember(9)]
        public int ImageAmountSeed;

        // events

        [ProtoMember(10)]
        public string Key = "";

        [ProtoMember(11)]
        public double MouseX;

        [ProtoMember(12)]
        public double MouseY;

        [ProtoMember(13)]
        public string MouseEvent = "";
    }


    //[ProtoContract]
    //public abstract class PayloadBase
    //{
    //    [ProtoMember(1)]
    //    public string Command { get; set; } = "";
    //}

    //[ProtoContract]
    //public class PayloadServer : PayloadBase 
    //{
    //    [ProtoMember(1)]
    //    public int Port { get; set; }

    //    [ProtoMember(2)]
    //    public int WindowHandle { get; set; }

    //    [ProtoMember(3)]
    //    public string WindowTitle { get; set; } = "";
    //}

    //[ProtoContract]
    //public class PayloadWindowShot : PayloadServer
    //{
    //    [ProtoMember(1)]
    //    public List<byte[]> WindowImagesInBytes { get; set; }

    //    // NOT SERIALIZED
    //    public Bitmap[,] WindowImages { get; set; }

    //    [ProtoMember(2)]
    //    public int WindowHeight { get; set; }

    //    [ProtoMember(3)]
    //    public int WindowWidth { get; set; }

    //    [ProtoMember(4)]
    //    public bool WindowIsFocused { get; set; } = false;

    //    [ProtoMember(5)]
    //    public int ImageAmountSeed { get; set; }
    //}

    //[ProtoContract]
    //public class PayloadEvent : PayloadBase
    //{
    //    [ProtoMember(1)]
    //    public string Key { get; set; } = "";

    //    [ProtoMember(2)]
    //    public double MouseX { get; set; }

    //    [ProtoMember(3)]
    //    public double MouseY { get; set; }

    //    [ProtoMember(4)]
    //    public string MouseEvent { get; set; } = "";
    //}
}
