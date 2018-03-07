﻿// Copyright 2018 Confluent Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// Refer to LICENSE for more information.

using System;
using Xunit;
using Moq;


namespace Confluent.Kafka.UnitTests
{
    /// <summary>
    ///     An example of using Moq with IProducer.
    ///     (not a test of Confluent.Kafka as such)
    /// </summary>
    public class MoqExampleTests
    {
        [Fact]
        public void IProducer()
        {
            Error storedProducerError = new Error(ErrorCode.NoError);

            var mock = new Mock<IProducer<Null, string>>();
            var producer = mock.Object;
            producer.OnError += (_, e)
                => storedProducerError = e;

            var produceCount = 0;
            var flushCount = 0;
            mock.Setup(m => m.Produce(It.IsAny<Action<Message<Null, string>>>(), It.IsAny<string>(), It.IsAny<Null>(), It.IsAny<string>()))
                .Callback<Action<Message<Null, string>>, string, Null, string>((action, topic, key, value) => 
                    {
                        var result = new Message<Null, string>(topic, 0, 0, key, value, Timestamp.Default, null, new Error(ErrorCode.NoError));
                        // Note: this is a simplification of the actual Producer implementation -
                        // A good mock would delay invocation of the callback and invoke it on a
                        // different thread.
                        action.Invoke(result);
                        produceCount += 1;
                    });
            mock.Setup(m => m.Flush(It.IsAny<TimeSpan>())).Returns(0).Callback(() => flushCount += 1);

            Message<Null, string> produced = null;
            producer.Produce((m) => produced = m, "my-topic", null, "my-value");
            var remaining = producer.Flush(TimeSpan.FromSeconds(10));

            Assert.Equal("my-topic", produced.Topic);
            Assert.Equal(1, produceCount);
            Assert.Equal(1, flushCount);
            Assert.Equal(0, remaining);

            mock.Raise(m => m.OnError += null, new object[] { new object(), new Error(ErrorCode.NotController) });

            Assert.Equal(storedProducerError, new Error(ErrorCode.NotController));
        }
    }
}
