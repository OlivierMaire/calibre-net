﻿using System;
using System.Collections.Generic;

namespace Calibre_net.Data.Calibre;

public partial class Annotation
{
    public int Id { get; set; }

    public int Book { get; set; }

    public string Format { get; set; } = null!;

    public string UserType { get; set; } = null!;

    public string User { get; set; } = null!;

    public double Timestamp { get; set; }

    public string AnnotId { get; set; } = null!;

    public string AnnotType { get; set; } = null!;

    public string AnnotData { get; set; } = null!;

    public string SearchableText { get; set; } = null!;
}
