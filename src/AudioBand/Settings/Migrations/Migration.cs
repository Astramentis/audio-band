﻿using System;
using System.Collections.Generic;
using System.Linq;
using AudioBand.Logging;
using NLog;

namespace AudioBand.Settings.Migrations
{
    /// <summary>
    /// Migrate settings from one version to another.
    /// </summary>
    internal static class Migration
    {
        private static readonly Dictionary<(string From, string To), ISettingsMigrator> SupportedMigrations = new Dictionary<(string From, string To), ISettingsMigrator>()
        {
            { ("0.1", "2"), new V1ToV2() },
            { ("2", "3"), new V2ToV3() },
        };

        private static readonly ILogger Logger = AudioBandLogManager.GetLogger(typeof(Migration).FullName);

        /// <summary>
        /// Migrate settings to new version.
        /// </summary>
        /// <typeparam name="TNew">Type of the new settings.</typeparam>
        /// <param name="oldSettings">The old settings.</param>
        /// <param name="oldVersion">The version of the old settings.</param>
        /// <param name="newVersion">The version of the new settings.</param>
        /// <returns>New settings.</returns>
        public static TNew MigrateSettings<TNew>(object oldSettings, string oldVersion, string newVersion)
        {
            if (oldVersion == newVersion)
            {
                return (TNew)oldSettings;
            }

            var plan = FindPlan(oldVersion, newVersion);
            if (!plan.Any())
            {
                throw new ArgumentException($"No migration plan from {oldVersion} to {newVersion}");
            }

            Logger.Debug("Found old settings v{old}. Migrating settings using {plan}", oldVersion, string.Join("->", plan));

            object settings = plan.Aggregate(oldSettings, (current, settingsMigrator) => settingsMigrator.MigrateSetting(current));
            return (TNew)settings;
        }

        private static List<ISettingsMigrator> FindPlan(string from, string to)
        {
            return SupportedMigrations.Where(x => x.Key.From == from && x.Key.To == to).Select(x => x.Value).ToList();
        }
    }
}
