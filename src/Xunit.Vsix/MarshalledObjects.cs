using System;
using System.Collections.Generic;
using System.Runtime.Remoting;

namespace Xunit;

class MarshalledObjects : IDisposable
{
    readonly List<MarshalByRefObject> marshalByRefs = new();

    public T Add<T>(T mbr) where T : MarshalByRefObject
    {
        marshalByRefs.Add(mbr);
        return mbr;
    }

    public void AddRange(IEnumerable<MarshalByRefObject> mbros)
        => marshalByRefs.AddRange(mbros);

    public void Dispose()
    {
        foreach (var mbro in marshalByRefs)
            if (!RemotingServices.IsTransparentProxy(mbro))
                RemotingServices.Disconnect(mbro);
    }
}
