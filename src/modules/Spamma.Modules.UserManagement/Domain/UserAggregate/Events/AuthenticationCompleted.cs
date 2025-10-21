﻿using Spamma.Modules.Common.Domain.Contracts;

namespace Spamma.Modules.UserManagement.Domain.UserAggregate.Events;

public record AuthenticationCompleted(Guid AuthenticationAttemptId, DateTime WhenCompleted, Guid SecurityStamp);