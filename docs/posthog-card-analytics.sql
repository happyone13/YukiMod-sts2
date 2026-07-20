-- YukiMod PostHog / HogQL card analytics
-- Source event: run_history.completed
-- Applicant id: YukiMod
-- Character filter: properties.run_character_ids contains YUKI
-- Time window: last 90 days
--
-- Notes:
-- 1. Victory and abandonment are read from event properties because the current
--    RunHistory payload samples do not reliably expose a useful is_victory field.
-- 2. Card reward data is read from:
--    properties.payload.applicant_payload.run_history.map_point_history[*][*]
--      .player_stats[*].card_choices[*]
-- 3. Final deck data is read from:
--    properties.payload.applicant_payload.run_history.players[*].deck[*]
-- 4. Card names below are generated from YukiMod/localization/zhs/cards.json.


-- 1) Overall Yuki run win rate, including abandoned runs.
SELECT
    count() AS run_count,
    countIf(is_victory) AS win_count,
    countIf(is_abandoned) AS abandoned_count,
    if(run_count = 0, 0, round(win_count / run_count * 100, 1)) AS win_rate_percent,
    if(run_count = 0, 0, round(abandoned_count / run_count * 100, 1)) AS abandoned_rate_percent
FROM (
    SELECT
        lower(toString(properties.is_victory)) IN ('true', '1') AS is_victory,
        lower(toString(properties.is_abandoned)) IN ('true', '1') AS is_abandoned
    FROM events
    WHERE event = 'run_history.completed'
      AND properties.applicant_id = 'YukiMod'
      AND properties.category = 'RunHistory'
      AND toString(properties.run_character_ids) LIKE '%YUKI%'
      AND timestamp >= now() - INTERVAL 90 DAY
);


-- 2) Overall Yuki run win rate, excluding abandoned runs.
SELECT
    count() AS run_count,
    countIf(is_victory) AS win_count,
    if(run_count = 0, 0, round(win_count / run_count * 100, 1)) AS win_rate_percent
FROM (
    SELECT
        lower(toString(properties.is_victory)) IN ('true', '1') AS is_victory,
        lower(toString(properties.is_abandoned)) IN ('true', '1') AS is_abandoned
    FROM events
    WHERE event = 'run_history.completed'
      AND properties.applicant_id = 'YukiMod'
      AND properties.category = 'RunHistory'
      AND toString(properties.run_character_ids) LIKE '%YUKI%'
      AND timestamp >= now() - INTERVAL 90 DAY
)
WHERE NOT is_abandoned;


-- 3) Card reward offer / pick / picked-run win rate.
--    offered_win_rate_percent: win rate of runs where this card was offered.
--    picked_win_rate_percent: win rate of runs where this card was picked.
SELECT
    card_id,
    card_name,
    count() AS offered_count,
    countIf(was_picked) AS picked_count,
    round(picked_count / offered_count * 100, 1) AS pick_rate_percent,
    uniq(run_id) AS offered_run_count,
    uniqIf(run_id, is_victory) AS offered_win_count,
    round(offered_win_count / offered_run_count * 100, 1) AS offered_win_rate_percent,
    uniqIf(run_id, was_picked) AS picked_run_count,
    uniqIf(run_id, was_picked AND is_victory) AS picked_win_count,
    if(picked_run_count = 0, 0, round(picked_win_count / picked_run_count * 100, 1)) AS picked_win_rate_percent
FROM (
    SELECT
        run_id,
        is_victory,
        replaceRegexpOne(JSONExtractString(card_choice, 'card', 'id'), '^CARD\\.', '') AS card_id,
        multiIf(
            card_id = 'YUKIMOD_AN_YUE', '暗月',
            card_id = 'YUKIMOD_BA_DAO', '拔刀',
            card_id = 'YUKIMOD_BA_DAO_ZHAN', '拔刀斩',
            card_id = 'YUKIMOD_BEI_SHUI', '背水',
            card_id = 'YUKIMOD_BI_AN_HUA', '彼岸花',
            card_id = 'YUKIMOD_BING_DIAN_ZHI_REN', '冰点之刃',
            card_id = 'YUKIMOD_BING_FENG', '冰封',
            card_id = 'YUKIMOD_BING_XUE', '冰雪',
            card_id = 'YUKIMOD_BLACK_CLOUD', '黑云',
            card_id = 'YUKIMOD_BO_RI_JIAN_YUN', '拨云见日',
            card_id = 'YUKIMOD_CAN_YUE', '残月',
            card_id = 'YUKIMOD_CHEN_SI', '沉思',
            card_id = 'YUKIMOD_COUNTS_AS_MOONSHADOW', '视为月影',
            card_id = 'YUKIMOD_DAO_QIAO_DA_JI', '刀鞘打击',
            card_id = 'YUKIMOD_DEFEND_YUKI', '防御',
            card_id = 'YUKIMOD_ER_SHI_TOKEN', '二式',
            card_id = 'YUKIMOD_FORESEE', '预见',
            card_id = 'YUKIMOD_GAO_SU_ZHAN_JI', '高速斩击',
            card_id = 'YUKIMOD_HAN_BING_BI_HU', '寒冰庇护',
            card_id = 'YUKIMOD_HAN_SHUANG', '寒霜',
            card_id = 'YUKIMOD_HEI_YUN_AO_YI_CAN', '黑云奥义：残',
            card_id = 'YUKIMOD_HEI_YUN_AO_YI_HEI_WU', '黑云奥义：黑雾',
            card_id = 'YUKIMOD_HEI_YUN_AO_YI_MIE', '黑云奥义：灭',
            card_id = 'YUKIMOD_HEI_YUN_AO_YI_SHANG', '黑云奥义：殇',
            card_id = 'YUKIMOD_HEI_YUN_MI_FA_CHU_QIAO', '黑云秘法：出鞘',
            card_id = 'YUKIMOD_HEI_YUN_MI_FA_HUANG_HUN', '黑云秘法：黄昏',
            card_id = 'YUKIMOD_HEI_YUN_MI_FA_HUN_YOU', '黑云秘法：魂佑',
            card_id = 'YUKIMOD_HEI_YUN_MI_FA_JI_YU', '黑云秘法：积雨',
            card_id = 'YUKIMOD_HEI_YUN_MI_FA_JIANG_LIN', '黑云秘法：降临',
            card_id = 'YUKIMOD_HEI_YUN_MI_FA_MU_LIN', '黑云秘法：幕临',
            card_id = 'YUKIMOD_HEI_YUN_MI_FA_SUN_WU', '黑云秘法：隼武',
            card_id = 'YUKIMOD_HEI_YUN_MI_FA_XIA_ZHEN', '黑云秘法：霞阵',
            card_id = 'YUKIMOD_HEI_YUN_MI_FA_XU_XIANG', '黑云秘法：虚像',
            card_id = 'YUKIMOD_HEI_YUN_MI_FA_YAN_HUI', '黑云秘法：燕返',
            card_id = 'YUKIMOD_HEI_YUN_MI_FA_YING_FU', '黑云奥义：影',
            card_id = 'YUKIMOD_HEI_YUN_MI_JI_HUI_ZHUAN', '黑云秘法：回转',
            card_id = 'YUKIMOD_HEI_YUN_XIN_FA', '黑云心法',
            card_id = 'YUKIMOD_HONG_CHEN', '红尘',
            card_id = 'YUKIMOD_HUA', '花',
            card_id = 'YUKIMOD_HUAN_BIAN_ZHAN', '幻变斩',
            card_id = 'YUKIMOD_HUANG_HUN_DE_JI_BAN', '黄昏的羁绊',
            card_id = 'YUKIMOD_HUI_NIAN', '回念',
            card_id = 'YUKIMOD_HUI_TIAN', '回天',
            card_id = 'YUKIMOD_INSPIRATION', '灵感',
            card_id = 'YUKIMOD_JIAN_WU', '剑舞',
            card_id = 'YUKIMOD_JIAN_XIN', '剑心',
            card_id = 'YUKIMOD_JU_HE', '居合',
            card_id = 'YUKIMOD_KAN_PO', '看破',
            card_id = 'YUKIMOD_KUAI_SU_ZHAN', '快速斩',
            card_id = 'YUKIMOD_LAN_YUE', '蓝月',
            card_id = 'YUKIMOD_LING_DU', '零度',
            card_id = 'YUKIMOD_LING_GUANG', '灵光',
            card_id = 'YUKIMOD_LING_SHI', '零式',
            card_id = 'YUKIMOD_LUN_HUI_ZHAN', '轮回斩',
            card_id = 'YUKIMOD_MAN_TIAN', '漫天',
            card_id = 'YUKIMOD_MAN_YUE', '满月',
            card_id = 'YUKIMOD_MI_HUO_YI_JI', '迷惑一击',
            card_id = 'YUKIMOD_MING_DING', '命定',
            card_id = 'YUKIMOD_NA_DAO', '纳刀',
            card_id = 'YUKIMOD_NEXT_ATTACK_PLAY_COUNT', '回天',
            card_id = 'YUKIMOD_NING_JU', '凝聚',
            card_id = 'YUKIMOD_NO_MING', '无明',
            card_id = 'YUKIMOD_NU_TAO_ZHAN', '怒涛斩',
            card_id = 'YUKIMOD_PO_BING_ZHAN', '破冰斩',
            card_id = 'YUKIMOD_QI_SHOU_SHI', '起手式',
            card_id = 'YUKIMOD_REN_GE_QIE_HUAN', '人格切换',
            card_id = 'YUKIMOD_REN_JIAN_HE_YI', '人剑合一',
            card_id = 'YUKIMOD_RONG_YU', '荣誉',
            card_id = 'YUKIMOD_RU_YING_SUI_XING', '如影随形',
            card_id = 'YUKIMOD_SAN_SHI_TOKEN', '三式',
            card_id = 'YUKIMOD_SHADOW_MOON', '影月',
            card_id = 'YUKIMOD_SHANG_XIAN_ZHI_YUE', '上弦之月',
            card_id = 'YUKIMOD_SHEN_YA_ZHI_ZHUN_BEI', '神丶压制准备',
            card_id = 'YUKIMOD_SHOU_YU', '授予',
            card_id = 'YUKIMOD_SHUANG_JIANG', '霜降',
            card_id = 'YUKIMOD_SHUN_NIAN', '瞬念',
            card_id = 'YUKIMOD_SHUO_YUE', '朔月',
            card_id = 'YUKIMOD_STRIKE_YUKI', '打击',
            card_id = 'YUKIMOD_TA_QIAN_ZHAN', '踏前斩',
            card_id = 'YUKIMOD_TIAN_DAO_XING_TAI', '天刀形态',
            card_id = 'YUKIMOD_TIAN_JI', '天际',
            card_id = 'YUKIMOD_TIAN_JI_ZHAN_JI', '天际斩击',
            card_id = 'YUKIMOD_TIAN_YAN', '天眼',
            card_id = 'YUKIMOD_TOU_XI_ZHAN', '偷袭斩',
            card_id = 'YUKIMOD_WEI_YA', '威压',
            card_id = 'YUKIMOD_WEI_YU_CHOU_MOU', '未雨绸缪',
            card_id = 'YUKIMOD_XIAN_JIAN_ZHI_MING', '先见之明',
            card_id = 'YUKIMOD_XIN_YUE', '新月',
            card_id = 'YUKIMOD_XUE', '雪',
            card_id = 'YUKIMOD_XUE_YING', '雪影',
            card_id = 'YUKIMOD_XUE_YUE', '血月',
            card_id = 'YUKIMOD_YA_ZHI_ZHUN_BEI', '压制准备',
            card_id = 'YUKIMOD_YAN_HAN', '严寒',
            card_id = 'YUKIMOD_YAN_HUI_FAN', '燕回反',
            card_id = 'YUKIMOD_YAO_GUAI_SHOU_LIE', '妖怪狩猎',
            card_id = 'YUKIMOD_YE_HUO', '业火',
            card_id = 'YUKIMOD_YI_JI_BI_SHA_JU_HE_CHOU_KA', '一击必杀！居合抽卡',
            card_id = 'YUKIMOD_YI_SHAN', '一闪',
            card_id = 'YUKIMOD_YI_SHI', '明镜止水',
            card_id = 'YUKIMOD_YI_SHI_TOKEN', '一式',
            card_id = 'YUKIMOD_YI_XIAN', '一现',
            card_id = 'YUKIMOD_YIN_LEI_TIAN_YUN', '引雷天云',
            card_id = 'YUKIMOD_YING_YUE', '盈月',
            card_id = 'YUKIMOD_YING_YUE_MIRROR', '映月',
            card_id = 'YUKIMOD_YONG_YE', '永夜',
            card_id = 'YUKIMOD_YUAN_WU', '圆舞',
            card_id = 'YUKIMOD_YUE', '月',
            card_id = 'YUKIMOD_YUE_DU', '月读',
            card_id = 'YUKIMOD_YUE_GUANG', '月光',
            card_id = 'YUKIMOD_YUE_YING', '月影',
            card_id = 'YUKIMOD_ZHAN_PO_MING_YUN', '斩破命运',
            card_id = 'YUKIMOD_ZHAO_JIA', '招架',
            card_id = 'YUKIMOD_ZHEN_DAO', '振刀',
            card_id = 'YUKIMOD_ZHOU_SHU', '咒术',
            card_id
        ) AS card_name,
        JSONExtractBool(card_choice, 'was_picked') AS was_picked
    FROM (
        SELECT
            run_id,
            is_victory,
            arrayJoin(JSONExtractArrayRaw(ifNull(JSONExtractRaw(player_stat, 'card_choices'), '[]'))) AS card_choice
        FROM (
            SELECT
                run_id,
                is_victory,
                arrayJoin(JSONExtractArrayRaw(ifNull(JSONExtractRaw(map_point, 'player_stats'), '[]'))) AS player_stat
            FROM (
                SELECT
                    run_id,
                    is_victory,
                    arrayJoin(JSONExtractArrayRaw(ifNull(act, '[]'))) AS map_point
                FROM (
                    SELECT
                        toString(uuid) AS run_id,
                        lower(toString(properties.is_victory)) IN ('true', '1') AS is_victory,
                        arrayJoin(JSONExtractArrayRaw(ifNull(
                            JSONExtractRaw(toString(coalesce(properties.payload, '{}')), 'applicant_payload', 'run_history', 'map_point_history'),
                            '[]'
                        ))) AS act
                    FROM events
                    WHERE event = 'run_history.completed'
                      AND properties.applicant_id = 'YukiMod'
                      AND properties.category = 'RunHistory'
                      AND toString(properties.run_character_ids) LIKE '%YUKI%'
                      AND timestamp >= now() - INTERVAL 90 DAY
                )
            )
        )
    )
)
WHERE card_id LIKE 'YUKIMOD_%'
GROUP BY card_id, card_name
ORDER BY offered_count DESC, pick_rate_percent DESC
LIMIT 200;


-- 4) Final deck card win rate.
--    This is a run-level metric: a run counts once for a card even if the deck
--    contains multiple copies. final_deck_copy_count still reports total copies.
SELECT
    card_id,
    card_name,
    count() AS final_deck_copy_count,
    uniq(run_id) AS run_count,
    uniqIf(run_id, is_victory) AS win_count,
    round(win_count / run_count * 100, 1) AS win_rate_percent,
    round(final_deck_copy_count / run_count, 2) AS avg_copies_when_present
FROM (
    SELECT
        run_id,
        is_victory,
        replaceRegexpOne(JSONExtractString(card_item, 'id'), '^CARD\\.', '') AS card_id,
        multiIf(
            card_id = 'YUKIMOD_AN_YUE', '暗月',
            card_id = 'YUKIMOD_BA_DAO', '拔刀',
            card_id = 'YUKIMOD_BA_DAO_ZHAN', '拔刀斩',
            card_id = 'YUKIMOD_BEI_SHUI', '背水',
            card_id = 'YUKIMOD_BI_AN_HUA', '彼岸花',
            card_id = 'YUKIMOD_BING_DIAN_ZHI_REN', '冰点之刃',
            card_id = 'YUKIMOD_BING_FENG', '冰封',
            card_id = 'YUKIMOD_BING_XUE', '冰雪',
            card_id = 'YUKIMOD_BLACK_CLOUD', '黑云',
            card_id = 'YUKIMOD_BO_RI_JIAN_YUN', '拨云见日',
            card_id = 'YUKIMOD_CAN_YUE', '残月',
            card_id = 'YUKIMOD_CHEN_SI', '沉思',
            card_id = 'YUKIMOD_COUNTS_AS_MOONSHADOW', '视为月影',
            card_id = 'YUKIMOD_DAO_QIAO_DA_JI', '刀鞘打击',
            card_id = 'YUKIMOD_DEFEND_YUKI', '防御',
            card_id = 'YUKIMOD_ER_SHI_TOKEN', '二式',
            card_id = 'YUKIMOD_FORESEE', '预见',
            card_id = 'YUKIMOD_GAO_SU_ZHAN_JI', '高速斩击',
            card_id = 'YUKIMOD_HAN_BING_BI_HU', '寒冰庇护',
            card_id = 'YUKIMOD_HAN_SHUANG', '寒霜',
            card_id = 'YUKIMOD_HEI_YUN_AO_YI_CAN', '黑云奥义：残',
            card_id = 'YUKIMOD_HEI_YUN_AO_YI_HEI_WU', '黑云奥义：黑雾',
            card_id = 'YUKIMOD_HEI_YUN_AO_YI_MIE', '黑云奥义：灭',
            card_id = 'YUKIMOD_HEI_YUN_AO_YI_SHANG', '黑云奥义：殇',
            card_id = 'YUKIMOD_HEI_YUN_MI_FA_CHU_QIAO', '黑云秘法：出鞘',
            card_id = 'YUKIMOD_HEI_YUN_MI_FA_HUANG_HUN', '黑云秘法：黄昏',
            card_id = 'YUKIMOD_HEI_YUN_MI_FA_HUN_YOU', '黑云秘法：魂佑',
            card_id = 'YUKIMOD_HEI_YUN_MI_FA_JI_YU', '黑云秘法：积雨',
            card_id = 'YUKIMOD_HEI_YUN_MI_FA_JIANG_LIN', '黑云秘法：降临',
            card_id = 'YUKIMOD_HEI_YUN_MI_FA_MU_LIN', '黑云秘法：幕临',
            card_id = 'YUKIMOD_HEI_YUN_MI_FA_SUN_WU', '黑云秘法：隼武',
            card_id = 'YUKIMOD_HEI_YUN_MI_FA_XIA_ZHEN', '黑云秘法：霞阵',
            card_id = 'YUKIMOD_HEI_YUN_MI_FA_XU_XIANG', '黑云秘法：虚像',
            card_id = 'YUKIMOD_HEI_YUN_MI_FA_YAN_HUI', '黑云秘法：燕返',
            card_id = 'YUKIMOD_HEI_YUN_MI_FA_YING_FU', '黑云奥义：影',
            card_id = 'YUKIMOD_HEI_YUN_MI_JI_HUI_ZHUAN', '黑云秘法：回转',
            card_id = 'YUKIMOD_HEI_YUN_XIN_FA', '黑云心法',
            card_id = 'YUKIMOD_HONG_CHEN', '红尘',
            card_id = 'YUKIMOD_HUA', '花',
            card_id = 'YUKIMOD_HUAN_BIAN_ZHAN', '幻变斩',
            card_id = 'YUKIMOD_HUANG_HUN_DE_JI_BAN', '黄昏的羁绊',
            card_id = 'YUKIMOD_HUI_NIAN', '回念',
            card_id = 'YUKIMOD_HUI_TIAN', '回天',
            card_id = 'YUKIMOD_INSPIRATION', '灵感',
            card_id = 'YUKIMOD_JIAN_WU', '剑舞',
            card_id = 'YUKIMOD_JIAN_XIN', '剑心',
            card_id = 'YUKIMOD_JU_HE', '居合',
            card_id = 'YUKIMOD_KAN_PO', '看破',
            card_id = 'YUKIMOD_KUAI_SU_ZHAN', '快速斩',
            card_id = 'YUKIMOD_LAN_YUE', '蓝月',
            card_id = 'YUKIMOD_LING_DU', '零度',
            card_id = 'YUKIMOD_LING_GUANG', '灵光',
            card_id = 'YUKIMOD_LING_SHI', '零式',
            card_id = 'YUKIMOD_LUN_HUI_ZHAN', '轮回斩',
            card_id = 'YUKIMOD_MAN_TIAN', '漫天',
            card_id = 'YUKIMOD_MAN_YUE', '满月',
            card_id = 'YUKIMOD_MI_HUO_YI_JI', '迷惑一击',
            card_id = 'YUKIMOD_MING_DING', '命定',
            card_id = 'YUKIMOD_NA_DAO', '纳刀',
            card_id = 'YUKIMOD_NEXT_ATTACK_PLAY_COUNT', '回天',
            card_id = 'YUKIMOD_NING_JU', '凝聚',
            card_id = 'YUKIMOD_NO_MING', '无明',
            card_id = 'YUKIMOD_NU_TAO_ZHAN', '怒涛斩',
            card_id = 'YUKIMOD_PO_BING_ZHAN', '破冰斩',
            card_id = 'YUKIMOD_QI_SHOU_SHI', '起手式',
            card_id = 'YUKIMOD_REN_GE_QIE_HUAN', '人格切换',
            card_id = 'YUKIMOD_REN_JIAN_HE_YI', '人剑合一',
            card_id = 'YUKIMOD_RONG_YU', '荣誉',
            card_id = 'YUKIMOD_RU_YING_SUI_XING', '如影随形',
            card_id = 'YUKIMOD_SAN_SHI_TOKEN', '三式',
            card_id = 'YUKIMOD_SHADOW_MOON', '影月',
            card_id = 'YUKIMOD_SHANG_XIAN_ZHI_YUE', '上弦之月',
            card_id = 'YUKIMOD_SHEN_YA_ZHI_ZHUN_BEI', '神丶压制准备',
            card_id = 'YUKIMOD_SHOU_YU', '授予',
            card_id = 'YUKIMOD_SHUANG_JIANG', '霜降',
            card_id = 'YUKIMOD_SHUN_NIAN', '瞬念',
            card_id = 'YUKIMOD_SHUO_YUE', '朔月',
            card_id = 'YUKIMOD_STRIKE_YUKI', '打击',
            card_id = 'YUKIMOD_TA_QIAN_ZHAN', '踏前斩',
            card_id = 'YUKIMOD_TIAN_DAO_XING_TAI', '天刀形态',
            card_id = 'YUKIMOD_TIAN_JI', '天际',
            card_id = 'YUKIMOD_TIAN_JI_ZHAN_JI', '天际斩击',
            card_id = 'YUKIMOD_TIAN_YAN', '天眼',
            card_id = 'YUKIMOD_TOU_XI_ZHAN', '偷袭斩',
            card_id = 'YUKIMOD_WEI_YA', '威压',
            card_id = 'YUKIMOD_WEI_YU_CHOU_MOU', '未雨绸缪',
            card_id = 'YUKIMOD_XIAN_JIAN_ZHI_MING', '先见之明',
            card_id = 'YUKIMOD_XIN_YUE', '新月',
            card_id = 'YUKIMOD_XUE', '雪',
            card_id = 'YUKIMOD_XUE_YING', '雪影',
            card_id = 'YUKIMOD_XUE_YUE', '血月',
            card_id = 'YUKIMOD_YA_ZHI_ZHUN_BEI', '压制准备',
            card_id = 'YUKIMOD_YAN_HAN', '严寒',
            card_id = 'YUKIMOD_YAN_HUI_FAN', '燕回反',
            card_id = 'YUKIMOD_YAO_GUAI_SHOU_LIE', '妖怪狩猎',
            card_id = 'YUKIMOD_YE_HUO', '业火',
            card_id = 'YUKIMOD_YI_JI_BI_SHA_JU_HE_CHOU_KA', '一击必杀！居合抽卡',
            card_id = 'YUKIMOD_YI_SHAN', '一闪',
            card_id = 'YUKIMOD_YI_SHI', '明镜止水',
            card_id = 'YUKIMOD_YI_SHI_TOKEN', '一式',
            card_id = 'YUKIMOD_YI_XIAN', '一现',
            card_id = 'YUKIMOD_YIN_LEI_TIAN_YUN', '引雷天云',
            card_id = 'YUKIMOD_YING_YUE', '盈月',
            card_id = 'YUKIMOD_YING_YUE_MIRROR', '映月',
            card_id = 'YUKIMOD_YONG_YE', '永夜',
            card_id = 'YUKIMOD_YUAN_WU', '圆舞',
            card_id = 'YUKIMOD_YUE', '月',
            card_id = 'YUKIMOD_YUE_DU', '月读',
            card_id = 'YUKIMOD_YUE_GUANG', '月光',
            card_id = 'YUKIMOD_YUE_YING', '月影',
            card_id = 'YUKIMOD_ZHAN_PO_MING_YUN', '斩破命运',
            card_id = 'YUKIMOD_ZHAO_JIA', '招架',
            card_id = 'YUKIMOD_ZHEN_DAO', '振刀',
            card_id = 'YUKIMOD_ZHOU_SHU', '咒术',
            card_id
        ) AS card_name
    FROM (
        SELECT
            run_id,
            is_victory,
            arrayJoin(JSONExtractArrayRaw(ifNull(JSONExtractRaw(player_item, 'deck'), '[]'))) AS card_item
        FROM (
            SELECT
                toString(uuid) AS run_id,
                lower(toString(properties.is_victory)) IN ('true', '1') AS is_victory,
                arrayJoin(JSONExtractArrayRaw(ifNull(
                    JSONExtractRaw(
                        ifNull(
                            JSONExtractRaw(
                                ifNull(
                                    JSONExtractRaw(
                                        coalesce(toString(properties.payload), '{}'),
                                        'applicant_payload'
                                    ),
                                    '{}'
                                ),
                                'run_history'
                            ),
                            '{}'
                        ),
                        'players'
                    ),
                    '[]'
                ))) AS player_item
            FROM events
            WHERE event = 'run_history.completed'
              AND properties.applicant_id = 'YukiMod'
              AND properties.category = 'RunHistory'
              AND toString(properties.run_character_ids) LIKE '%YUKI%'
              AND timestamp >= now() - INTERVAL 90 DAY
        )
    )
)
WHERE card_id LIKE 'YUKIMOD_%'
GROUP BY card_id, card_name
HAVING run_count >= 1
ORDER BY win_rate_percent DESC, run_count DESC
LIMIT 200;
