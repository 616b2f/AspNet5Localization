using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Localization.JsonLocalizer.StringLocalizer
{
    public class JsonStringLocalizerTest
    {
        [Fact]
        public void CreateWithNullBaseName()
        {
            var logger = new Mock<ILogger>();
            var ex = Assert.Throws<ArgumentNullException>(() => new JsonStringLocalizer(null, "", logger.Object));
            Assert.Equal(ex.ParamName, "baseName");
        }
    }
}
