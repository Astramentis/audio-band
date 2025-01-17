﻿using AudioBand.AudioSource;
using AudioBand.Models;
using AudioBand.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using AudioBand.Messages;

namespace AudioBand.Test
{
    [TestClass]
    public class AudioSourceSettingsCollectionTests
    {
        private Mock<IInternalAudioSource> _audioSourceMock;
        private Mock<IMessageBus> _messageBus;

        [TestInitialize]
        public void Init()
        {
            _audioSourceMock = new Mock<IInternalAudioSource>();
            _messageBus = new Mock<IMessageBus>();
        }

        [TestMethod]
        public void NoMatchingSettings()
        {
            _audioSourceMock.SetupGet(s => s.Settings).Returns(new List<AudioSourceSettingAttribute>());
            var name = "test";
            _audioSourceMock.SetupGet(x => x.Name).Returns(name);

            var keyVals = new List<AudioSourceSetting>
            {
                new AudioSourceSetting { Name = "key1", Value = "val1" },
                new AudioSourceSetting { Name = "key2", Value = "val2" }
            };
            var settings = new AudioSourceSettings { AudioSourceName = name, Settings = keyVals };

            var vm = new AudioSourceSettingsCollectionViewModel(_audioSourceMock.Object, settings, _messageBus.Object);

            Assert.AreEqual(0, vm.SettingsList.Count);
            Assert.AreEqual(name, vm.AudioSourceName);
        }

        [TestMethod]
        public void MatchingSettingsShouldCreateVmsInOrder()
        {
            var setting1 = "Setting1";
            var setting2 = "setting2";
            object val1 = "val1";
            object val2 = 2;

            _audioSourceMock.SetupGet(s => s.Settings).Returns(new List<AudioSourceSettingAttribute>
            {
                new AudioSourceSettingAttribute(setting1),
                new AudioSourceSettingAttribute(setting2),
            });

            _audioSourceMock.SetupGet(s => s[It.Is<string>(x => x == setting1)]).Returns(val1);
            _audioSourceMock.SetupGet(s => s[It.Is<string>(x => x == setting2)]).Returns(val2);
            _audioSourceMock.Setup(s => s.GetSettingType(It.Is<string>(x => x == setting1))).Returns(typeof(string));
            _audioSourceMock.Setup(s => s.GetSettingType(It.Is<string>(x => x == setting2))).Returns(typeof(int));

            var settingModels = new List<AudioSourceSetting>
            {
                new AudioSourceSetting { Name = setting1, Value = val1 },
                new AudioSourceSetting { Name = setting2, Value = val2 }
            };

            var settings = new AudioSourceSettings { Settings = settingModels };
            var vm = new AudioSourceSettingsCollectionViewModel(_audioSourceMock.Object, settings, _messageBus.Object);

            Assert.AreEqual(settingModels.Count, vm.SettingsList.Count);
            Assert.AreEqual(settingModels[0].Name, vm.SettingsList[0].Name);
            Assert.AreEqual(settingModels[1].Name, vm.SettingsList[1].Name);

            Assert.AreEqual(settingModels[0].Value, vm.SettingsList[0].Value);
            Assert.AreEqual(settingModels[1].Value, vm.SettingsList[1].Value);
        }

        [TestMethod]
        public void AudioSourceSettingUpdateNewValueIsWrittenBackToSettings()
        {
            var setting = "setting";

            _audioSourceMock.SetupGet(s => s.Settings).Returns(new List<AudioSourceSettingAttribute> { new AudioSourceSettingAttribute(setting) });
            var settingModel = new AudioSourceSetting { Name = setting };
            var settings = new AudioSourceSettings
            {
                Settings = new List<AudioSourceSetting> { settingModel }
            };
            object newSettingValue = 1;
            _audioSourceMock.SetupGet(s => s[It.Is<string>(x => x == setting)]).Returns(newSettingValue);

            var vm = new AudioSourceSettingsCollectionViewModel(_audioSourceMock.Object, settings, _messageBus.Object);

            _audioSourceMock.Raise(s => s.SettingChanged += null, new SettingChangedEventArgs(setting));

            vm.EndEdit();
            Assert.AreEqual(newSettingValue, settingModel.Value);
        }
       
        [TestMethod]
        public void AudioSourceSettingsCalledInPriority()
        {
            var setting1 = new AudioSourceSettingAttribute("test1") { Priority = 10 };
            var setting2 = new AudioSourceSettingAttribute("test2") { Priority = 5 };
            var setting3 = new AudioSourceSettingAttribute("test3") { Priority = 20 };

            _audioSourceMock.SetupAllProperties();
            _audioSourceMock.SetupGet(m => m.Settings).Returns(new List<AudioSourceSettingAttribute> { setting1, setting2, setting3 });

            var settings = new AudioSourceSettings
            {
                Settings = new List<AudioSourceSetting>
                {
                    new AudioSourceSetting {Name = setting1.Name},
                    new AudioSourceSetting {Name = setting2.Name},
                    new AudioSourceSetting {Name = setting3.Name},
                }
            };

            var s = new MockSequence();
            _audioSourceMock.InSequence(s).SetupSet(source => source[It.Is<string>(x => x == setting3.Name)] = null);
            _audioSourceMock.InSequence(s).SetupSet(source => source[It.Is<string>(x => x == setting1.Name)] = null);
            _audioSourceMock.InSequence(s).SetupSet(source => source[It.Is<string>(x => x == setting2.Name)] = null);

            var vm = new AudioSourceSettingsCollectionViewModel(_audioSourceMock.Object, settings, _messageBus.Object);

            _audioSourceMock.VerifySet(source => source[setting3.Name] = null);
            _audioSourceMock.VerifySet(source => source[setting1.Name] = null);
            _audioSourceMock.VerifySet(source => source[setting2.Name] = null);
        }
    }      
}
