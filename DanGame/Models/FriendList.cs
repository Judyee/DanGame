﻿using System;
using System.Collections.Generic;

namespace DanGame.Models;

public partial class FriendList
{
    public int FriendshipId { get; set; }

    public int UserId { get; set; }

    public int FriendUserId { get; set; }

    public string Status { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
