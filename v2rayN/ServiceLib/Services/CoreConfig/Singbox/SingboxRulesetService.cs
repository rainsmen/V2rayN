namespace ServiceLib.Services.CoreConfig;

public partial class CoreConfigSingboxService
{
    private void ConvertGeo2Ruleset()
    {
        static void AddRuleSets(List<string> ruleSets, List<string>? rule_set)
        {
            if (rule_set != null)
            {
                ruleSets.AddRange(rule_set);
            }
        }
        var geosite = "geosite";
        var geoip = "geoip";
        var ruleSets = new List<string>();

        //convert route geosite & geoip to ruleset
        foreach (var rule in _coreConfig.route.rules.Where(t => t.geosite?.Count > 0).ToList() ?? [])
        {
            rule.rule_set ??= [];
            rule.rule_set.AddRange(rule?.geosite?.Select(t => $"{geosite}-{t}").ToList() ?? []);
            rule.geosite = null;
            AddRuleSets(ruleSets, rule.rule_set);
        }
        foreach (var rule in _coreConfig.route.rules.Where(t => t.geoip?.Count > 0).ToList() ?? [])
        {
            rule.rule_set ??= [];
            rule.rule_set.AddRange(rule?.geoip?.Select(t => $"{geoip}-{t}").ToList() ?? []);
            rule.geoip = null;
            AddRuleSets(ruleSets, rule.rule_set);
        }

        //convert dns geosite & geoip to ruleset
        foreach (var rule in _coreConfig.dns?.rules.Where(t => t.geosite?.Count > 0).ToList() ?? [])
        {
            rule.rule_set ??= [];
            rule.rule_set.AddRange(rule?.geosite?.Select(t => $"{geosite}-{t}").ToList() ?? []);
            rule.geosite = null;
        }
        foreach (var rule in _coreConfig.dns?.rules.Where(t => t.geoip?.Count > 0).ToList() ?? [])
        {
            rule.rule_set ??= [];
            rule.rule_set.AddRange(rule?.geoip?.Select(t => $"{geoip}-{t}").ToList() ?? []);
            rule.geoip = null;
        }
        foreach (var dnsRule in _coreConfig.dns?.rules.Where(t => t.rule_set?.Count > 0).ToList() ?? [])
        {
            AddRuleSets(ruleSets, dnsRule.rule_set);
        }
        //rules in rules
        foreach (var item in _coreConfig.dns?.rules.Where(t => t.rules?.Count > 0).Select(t => t.rules).ToList() ?? [])
        {
            foreach (var item2 in item ?? [])
            {
                AddRuleSets(ruleSets, item2.rule_set);
            }
        }
        //convert route rule_set to ruleset
        foreach (var rule in _coreConfig.route.rules.Where(t => t.rule_set?.Count > 0).ToList() ?? [])
        {
            AddRuleSets(ruleSets, rule.rule_set);
        }
        foreach (var item in _coreConfig.route.rules.Where(t => t.rules?.Count > 0).Select(t => t.rules).ToList() ?? [])
        {
            foreach (var item2 in item ?? [])
            {
                AddRuleSets(ruleSets, item2.rule_set);
            }
        }

        //load custom ruleset file
        List<Ruleset4Sbox> customRulesets = [];

        var routing = context.RoutingItem;
        if (routing.CustomRulesetPath4Singbox.IsNotEmpty())
        {
            var result = EmbedUtils.LoadResource(routing.CustomRulesetPath4Singbox);
            if (result.IsNotEmpty())
            {
                customRulesets = (JsonUtils.Deserialize<List<Ruleset4Sbox>>(result) ?? [])
                    .Where(t => t.tag != null)
                    .Where(t => t.type != null)
                    .Where(t => t.format != null)
                    .ToList();
            }
        }

        //Append ad-block rule sets when enabled
        if (_config.RoutingBasicItem.EnableAdBlock)
        {
            foreach (var kvp in Global.AdBlockRulesetUrls)
            {
                if (!ruleSets.Contains(kvp.Key))
                {
                    ruleSets.Add(kvp.Key);
                }
            }
        }

        //Local srs files address
        var localSrss = Utils.GetBinPath("srss");

        //Add ruleset srs
        _coreConfig.route.rule_set = [];
        var addedTags = new HashSet<string>();

        foreach (var crs in customRulesets)
        {
            if (crs.tag.IsNotEmpty() && addedTags.Add(crs.tag))
            {
                _coreConfig.route.rule_set.Add(crs);
            }
        }

        foreach (var item in new HashSet<string>(ruleSets))
        {
            if (item.IsNullOrEmpty() || addedTags.Contains(item))
            { continue; }
            var customRuleset = customRulesets.FirstOrDefault(t => t.tag != null && t.tag.Equals(item));
            if (customRuleset is null)
            {
                var pathSrs = Path.Combine(localSrss, $"{item}.srs");
                if (IsValidSrsFile(pathSrs))
                {
                    customRuleset = new()
                    {
                        type = "local",
                        format = "binary",
                        tag = item,
                        path = pathSrs
                    };
                }
                else
                {
                    if (File.Exists(pathSrs))
                    {
                        try
                        {
                            File.Delete(pathSrs);
                        }
                        catch
                        {
                            // ignore deletion failure
                        }
                    }

                    var srsUrl = string.IsNullOrEmpty(_config.ConstItem.SrsSourceUrl)
                        ? Global.SingboxRulesetUrl
                        : _config.ConstItem.SrsSourceUrl;

                    customRuleset = new()
                    {
                        type = "remote",
                        format = "binary",
                        tag = item,
                        url = string.Format(srsUrl, item.StartsWith(geosite) ? geosite : geoip, item),
                        download_detour = Global.ProxyTag
                    };
                }
            }
            _coreConfig.route.rule_set.Add(customRuleset);
            addedTags.Add(item);
        }
    }

    private static bool IsValidSrsFile(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return false;
            }
            var fi = new FileInfo(path);
            if (fi.Length < 16)
            {
                return false;
            }
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var header = new byte[4];
            if (fs.Read(header, 0, 4) != 4)
            {
                return false;
            }
            return header[0] == 0x53 && header[1] == 0x52 && header[2] == 0x53 && header[3] == 0x01;
        }
        catch
        {
            return false;
        }
    }
}
