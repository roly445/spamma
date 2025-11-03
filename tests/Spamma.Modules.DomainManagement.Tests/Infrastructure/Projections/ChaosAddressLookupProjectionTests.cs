using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using Marten;
using Spamma.Modules.DomainManagement.Domain.ChaosAddressAggregate.Events;
using Spamma.Modules.DomainManagement.Infrastructure.Projections;
using Spamma.Modules.DomainManagement.Infrastructure.ReadModels;
using JasperFx.Events;
using Xunit;

namespace Spamma.Modules.DomainManagement.Tests.Infrastructure.Projections;

public class ChaosAddressLookupProjectionTests
{
    private class FakePatchExpression<T> : Marten.Patching.IPatchExpression<T>
    {
        public Guid StreamId { get; }
        public readonly List<(string Property, object Value)> Sets = new();

        public FakePatchExpression(Guid streamId)
        {
            StreamId = streamId;
        }

        public Marten.Patching.IPatchExpression<T> Set<TProp>(Expression<Func<T, TProp>> property, TProp value)
        {
            var member = (property.Body as MemberExpression)!.Member.Name;
            Sets.Add((member, value!));
            return this;
        }

        public Marten.Patching.IPatchExpression<T> Increment(Expression<Func<T, int>> property, int amount)
        {
            var member = (property.Body as MemberExpression)!.Member.Name;
            Sets.Add((member, amount));
            return this;
        }

        // Other IPatchExpression members not used by projection
        public Marten.Patching.IPatchExpression<T> SetJson(string json) => this;
        public Marten.Patching.IPatchExpression<T> Add<TValue>(Expression<Func<T, IEnumerable<TValue>>> property, TValue value) => this;
    }

    private class FakeDocumentOperations : IDocumentOperations
    {
        public readonly Dictionary<Guid, object> Patches = new();

        public Marten.Patching.IPatchExpression<T> Patch<T>(Guid id)
        {
            var patch = new FakePatchExpression<T>(id);
            Patches[id] = patch;
            return patch;
        }

        #region NotImplemented IDocumentOperations
        public void Store<T>(T document) => throw new NotImplementedException();
        public void Delete<T>(T id) => throw new NotImplementedException();
        public void Delete<T>(Expression<Func<T, bool>> expression) => throw new NotImplementedException();
        public void Delete<T>(Guid id) => throw new NotImplementedException();
        public void Insert<T>(T document) => throw new NotImplementedException();
        public void Insert<T>(IEnumerable<T> documents) => throw new NotImplementedException();
        public void Store<T>(T document, Guid? id) => throw new NotImplementedException();
        public void Store<T>(T document, Guid id) => throw new NotImplementedException();
        public void Upsert<T>(T document) => throw new NotImplementedException();
        public void Upsert<T>(IEnumerable<T> documents) => throw new NotImplementedException();
        public Marten.Linq.IQuerySession QuerySession => throw new NotImplementedException();
        public void SaveChanges() => throw new NotImplementedException();
        #endregion
    }

    [Fact]
    public void Apply_ChaosAddressSubdomainChanged_PatchesReadModel()
    {
        var streamId = Guid.NewGuid();
        var newDomain = Guid.NewGuid();
        var newSubdomain = Guid.NewGuid();

        var evtMock = new Mock<IEvent<ChaosAddressSubdomainChanged>>();
        evtMock.SetupGet(x => x.StreamId).Returns(streamId);
        evtMock.SetupGet(x => x.Data).Returns(new ChaosAddressSubdomainChanged(newDomain, newSubdomain));

        var docOps = new FakeDocumentOperations();

        var projection = new ChaosAddressLookupProjection();
        projection.Project(evtMock.Object, docOps);

        docOps.Patches.Should().ContainKey(streamId);
        var patch = (FakePatchExpression<ChaosAddressLookup>)docOps.Patches[streamId];
        patch.Sets.Should().ContainSingle(s => s.Property == "DomainId" && (Guid)s.Value == newDomain);
        patch.Sets.Should().ContainSingle(s => s.Property == "SubdomainId" && (Guid)s.Value == newSubdomain);
    }
}
