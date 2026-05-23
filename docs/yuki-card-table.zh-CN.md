# Yuki 卡牌实现表

说明：
- 卡牌行由 YukiModCode/Cards 中挂到 YukiModCardPool 或 TokenCardPool 的卡牌类生成。
- 来自卡牌构造函数。费用记录基础费用数字。
- 来自卡牌类的 YukiCardSchool；未重写则记为“其他”。
- 来自 YukiMod/localization/zhs/cards.json，并按源码 DynamicVars 尽量替换基础/升级数值；未能静态展开的运行时变量保留 {变量名}。
- Token 牌也列入本表，但牌池标记为 Token牌池。
- 评分不是游戏运行内容，本次不从旧表沿用，避免错误分数伪装成当前实现。

## 卡牌表

| 名称 | 费用 | 稀有度 | 种类 | 效果 | 流派 | 备注 | 评分 |
| --- | ---: | --- | --- | --- | --- | --- | ---: |
| 压制准备 | 0 | 初始 | 技能 | 从抽牌堆或弃牌堆随机抽1张攻击牌。；获得2/5层活力。；灵感：额外抽1张。 | 灵感 | ID: YUKIMOD-YA_ZHI_ZHUN_BEI；类: YaZhiZhunBei；牌池: 主牌池 |  |
| 拔刀 | 0 | 初始 | 攻击 | 造成4/7点伤害。；黑云：额外攻击1次；否则，进入黑云姿态。 | 黑云 | ID: YUKIMOD-BA_DAO；类: BaDao；牌池: 主牌池；源码关键词: Retain |  |
| 打击 | 1 | 初始 | 攻击 | 造成6/9点伤害。 | 其他 | ID: YUKIMOD-STRIKE_YUKI；类: StrikeYuki；牌池: 主牌池 |  |
| 防御 | 1 | 初始 | 技能 | 获得5/8点格挡。 | 其他 | ID: YUKIMOD-DEFEND_YUKI；类: DefendYuki；牌池: 主牌池 |  |
| 严寒 | 1 | 罕见 | 攻击 | 造成5/8点伤害。；抽1张牌。；灵感：额外释放1次。 | 灵感 | ID: YUKIMOD-YAN_HAN；类: YanHan；牌池: 主牌池 |  |
| 偷袭斩 | 2 | 通常 | 攻击 | 对所有敌人造成15点伤害。；灵感：这张牌费用减少1/2。 | 灵感 | ID: YUKIMOD-TOU_XI_ZHAN；类: TouXiZhan；牌池: 主牌池 |  |
| 冰封 | 1 | 通常 | 攻击 | 造成8/11点伤害。；施加1层虚弱。；灵感：额外打出1次。 | 灵感 | ID: YUKIMOD-BING_FENG；类: BingFeng；牌池: 主牌池 |  |
| 快速斩 | 1 | 通常 | 攻击 | 造成2/3点伤害3次。；灵感：这张牌费用减少1。 | 灵感 | ID: YUKIMOD-KUAI_SU_ZHAN；类: KuaiSuZhan；牌池: 主牌池 |  |
| 斩破命运 | 1 | 通常 | 攻击 | 造成7/9点伤害。；预见2/3。；抽1张牌。 | 灵感 | ID: YUKIMOD-ZHAN_PO_MING_YUN；类: ZhanPoMingYun；牌池: 主牌池 |  |
| 迷惑一击 | 1 | 通常 | 攻击 | 造成8点伤害。；{IfUpgraded:show:选择1张牌触发灵感。\|随机1张牌触发灵感。} | 灵感 | ID: YUKIMOD-MI_HUO_YI_JI；类: MiHuoYiJi；牌池: 主牌池 |  |
| 雪 | 1 | 通常 | 攻击 | 造成6/9点伤害。；获得1层雪。；灵感：抽1张牌。 | 灵感 | ID: YUKIMOD-XUE；类: Xue；牌池: 主牌池 |  |
| 高速斩击 | 2 | 罕见 | 攻击 | 对所有敌人造成12点伤害。；抽2/3张牌。；灵感：这张牌费用减少1。 | 灵感 | ID: YUKIMOD-GAO_SU_ZHAN_JI；类: GaoSuZhanJi；牌池: 主牌池 |  |
| 一现 | 1 | 通常 | 技能 | 抽2/3张牌。；灵感：抽1张牌。 | 灵感 | ID: YUKIMOD-YI_XIAN；类: YiXian；牌池: 主牌池 |  |
| 天眼 | 1 | 通常 | 技能 | 获得7/9点格挡。；预见3/5。 | 灵感 | ID: YUKIMOD-TIAN_YAN；类: TianYan；牌池: 主牌池 |  |
| 寒冰庇护 | 1 | 罕见 | 技能 | 获得7/10点格挡。；抽1张牌。；灵感：再抽1张牌。 | 灵感 | ID: YUKIMOD-HAN_BING_BI_HU；类: HanBingBiHu；牌池: 主牌池 |  |
| 咒术 | 1 | 罕见 | 技能 | 升级你手中的月影。；凝聚1。 | 月影 | ID: YUKIMOD-ZHOU_SHU；类: ZhouShu；牌池: 主牌池；源码关键词: Exhaust；升级费用变化: -1 |  |
| 圆舞 | 1 | 通常 | 技能 | 获得8点格挡。；你手中的月影伤害+3/5。 | 月影 | ID: YUKIMOD-YUAN_WU；类: YuanWu；牌池: 主牌池 |  |
| 月光 | 1 | 通常 | 技能 | 凝聚1。；你手中的月影伤害+5/8。 | 月影 | ID: YUKIMOD-YUE_GUANG；类: YueGuang；牌池: 主牌池 |  |
| 花 | 1 | 通常 | 攻击 | 造成6/9点伤害。；获得1层花。；黑云：额外攻击1次。 | 黑云 | ID: YUKIMOD-HUA；类: Hua；牌池: 主牌池 |  |
| 黑云奥义：影 | 1 | 通常 | 攻击 | 造成3/5点伤害2次。；黑云：额外攻击2次；否则，获得2层黑云。 | 黑云 | ID: YUKIMOD-HEI_YUN_MI_FA_YING_FU；类: HeiYunMiFaYingFu；牌池: 主牌池 |  |
| 黑云奥义：殇 | 1 | 通常 | 攻击 | 造成9/12点伤害。；黑云：施加2层易伤；否则，获得2层黑云。 | 黑云 | ID: YUKIMOD-HEI_YUN_AO_YI_SHANG；类: HeiYunAoYiShang；牌池: 无卡池 |  |
| 黑云奥义：灭 | 1 | 通常 | 攻击 | 造成9/12点伤害。；黑云：额外攻击1次；否则，施加1层虚弱。 | 黑云 | ID: YUKIMOD-HEI_YUN_AO_YI_MIE；类: HeiYunAoYiMie；牌池: 主牌池 |  |
| 拨云见日 | 1 | 罕见 | 技能 | 获得2/3层黑云。；在你的下回合开始时，进入黑云姿态。 | 黑云 | ID: YUKIMOD-BO_RI_JIAN_YUN；类: BoRiJianYun；牌池: 主牌池 |  |
| 黑云奥义：残 | 0 | 罕见 | 技能 | 抽1张牌。；进入黑云姿态。 | 黑云 | ID: YUKIMOD-HEI_YUN_AO_YI_CAN；类: HeiYunAoYiCan；牌池: 主牌池；源码关键词: Exhaust；升级移除消耗 |  |
| 黑云秘法：积雨 | 1 | 通常 | 技能 | 获得8/11点格挡。；在你的下回合开始时，进入黑云姿态。 | 黑云 | ID: YUKIMOD-HEI_YUN_MI_FA_JI_YU；类: HeiYunMiFaJiYu；牌池: 主牌池 |  |
| 黑云秘法：隼武 | 1 | 通常 | 技能 | 获得5/8点格挡。；黑云：获得5/8点格挡；否则，下次进入黑云姿态时抽2张牌。 | 黑云 | ID: YUKIMOD-HEI_YUN_MI_FA_SUN_WU；类: HeiYunMiFaSunWu；牌池: 无卡池 |  |
| 黑云秘法：魂佑 | 1 | 通常 | 技能 | 获得5/8点格挡。；黑云：获得5/8点格挡；否则，下次进入黑云姿态时获得1。 | 黑云 | ID: YUKIMOD-HEI_YUN_MI_FA_HUN_YOU；类: HeiYunMiFaHunYou；牌池: 主牌池 |  |
| 刀鞘打击 | 1 | 通常 | 攻击 | 造成8/11点伤害。；在下个回合获得1。 | 其他 | ID: YUKIMOD-DAO_QIAO_DA_JI；类: DaoQiaoDaJi；牌池: 主牌池 |  |
| 妖怪狩猎 | 1 | 通常 | 攻击 | 造成2点伤害2/3次。；凝聚1。 | 其他 | ID: YUKIMOD-YAO_GUAI_SHOU_LIE；类: YaoGuaiShouLie；牌池: 主牌池 |  |
| 彼岸花 | 1 | 罕见 | 攻击 | 造成6/9点伤害。\n在接下来的2个回合开始时，造成6/9点伤害。 | 其他 | ID: YUKIMOD-BI_AN_HUA；类: BiAnHua；牌池: 主牌池 |  |
| 斩钢闪 | 1 | 通常 | 攻击 | 造成9/12点伤害。；连续3回合打出斩钢闪，将一张居合加入手牌。 | 其他 | ID: YUKIMOD-ZHAN_GANG_SHAN；类: ZhanGangShan；牌池: 主牌池 |  |
| 明镜止水 | 1 | 罕见 | 攻击 | 造成6/9点伤害。；你的下一张攻击牌费用减少1。 | 其他 | ID: YUKIMOD-YI_SHI；类: YiShi；牌池: 主牌池 |  |
| 月 | 1 | 通常 | 攻击 | 造成6/9点伤害。；获得1层月。；你手中的月影伤害+3。 | 其他 | ID: YUKIMOD-YUE；类: Yue；牌池: 主牌池 |  |
| 轮回斩 | 3 | 罕见 | 攻击 | 造成5点伤害3/4次。\n每次抽到时，费用在本场战斗中减少1。 | 其他 | ID: YUKIMOD-LUN_HUI_ZHAN；类: LunHuiZhan；牌池: 主牌池 |  |
| 一击必杀！居合抽卡 | 0 | 通常 | 技能 | 选择1张手牌弃掉。；抽1张牌。；如果抽到的是一击必杀！居合抽卡，将{IfUpgraded:show:1张居合+\|1张居合}加入手中。 | 其他 | ID: YUKIMOD-YI_JI_BI_SHA_JU_HE_CHOU_KA；类: YiJiBiShaJuHeChouKa；牌池: 主牌池 |  |
| 人剑合一 | 1 | 罕见 | 技能 | 获得8/11点格挡。；在接下来的2个回合开始时，各额外抽1张牌。 | 其他 | ID: YUKIMOD-REN_JIAN_HE_YI；类: RenJianHeYi；牌池: 主牌池 |  |
| 人格切换 | 0 | 罕见 | 技能 | 将2/1张手牌放回抽牌堆顶部。；抽2张牌。 | 其他 | ID: YUKIMOD-REN_GE_QIE_HUAN；类: RenGeQieHuan；牌池: 主牌池 |  |
| 剑心 | 0 | 罕见 | 技能 | 消耗1/2张手牌。；抽等量的牌。 | 其他 | ID: YUKIMOD-JIAN_XIN；类: JianXin；牌池: 主牌池 |  |
| 回念 | 1 | 罕见 | 技能 | 获得7/10点格挡。；将你弃牌堆中的1张牌放到抽牌堆顶部。 | 其他 | ID: YUKIMOD-HUI_NIAN；类: HuiNian；牌池: 主牌池 |  |
| 招架 | 1 | 罕见 | 技能 | 获得5/8点格挡。；本回合每打出一张牌，额外获得1点格挡（当前获得{CurrentBlock}点）。；如果刚好抵挡全部伤害，下回合开始时获得一张居合。 | 其他 | ID: YUKIMOD-ZHAO_JIA；类: ZhaoJia；牌池: 主牌池 |  |
| 未雨绸缪 | 1 | 罕见 | 技能 | 获得5/8点格挡。；抽2张牌。；将1张手牌置于抽牌堆顶部。 | 其他 | ID: YUKIMOD-WEI_YU_CHOU_MOU；类: WeiYuChouMou；牌池: 主牌池 |  |
| 冰雪 | 1 | 罕见 | 攻击 | 造成4点伤害2/3次。；抽1张牌，优先抽灵感牌。 | 灵感 | ID: YUKIMOD-BING_XUE；类: BingXue；牌池: 主牌池 |  |
| 破冰斩 | 1 | 罕见 | 攻击 | 选择1张手牌消耗。；对所有敌人造成7/10点伤害。；灵感：额外攻击一次。 | 灵感 | ID: YUKIMOD-PO_BING_ZHAN；类: PoBingZhan；牌池: 主牌池 |  |
| 命定 | 1 | 罕见 | 技能 | 预见3/5。；抽2张牌。 | 灵感 | ID: YUKIMOD-MING_DING；类: MingDing；牌池: 主牌池 |  |
| 天际 | 0 | 罕见 | 技能 | 把你的手牌放到抽牌堆顶部。；抽等量的牌。；灵感：抽1张牌。 | 灵感 | ID: YUKIMOD-TIAN_JI；类: TianJi；牌池: 主牌池；源码关键词: Exhaust；升级移除消耗 |  |
| 灵光 | 0 | 罕见 | 技能 | 抽2/3张牌。 | 灵感 | ID: YUKIMOD-LING_GUANG；类: LingGuang；牌池: 主牌池；源码关键词: Exhaust |  |
| 先见之明 | 1 | 罕见 | 能力 | 回合开始时，预见3/4。 | 灵感 | ID: YUKIMOD-XIAN_JIAN_ZHI_MING；类: XianJianZhiMing；牌池: 主牌池 |  |
| 冰点之刃 | 1 | 罕见 | 能力 | 当灵感触发时，对随机敌人造成4/6点伤害。 | 灵感 | ID: YUKIMOD-BING_DIAN_ZHI_REN；类: BingDianZhiRen；牌池: 主牌池 |  |
| 黑云秘法：虚像 | 1 | 罕见 | 技能 | 施加2/3层虚弱。；获得2/3层黑云。 | 黑云 | ID: YUKIMOD-HEI_YUN_MI_FA_XU_XIANG；类: HeiYunMiFaXuXiang；牌池: 无卡池 |  |
| 黑云秘法：降临 | 1 | 罕见 | 技能 | 获得8/11点格挡。；下次进入黑云姿态时，对所有敌人施加1层易伤。 | 黑云 | ID: YUKIMOD-HEI_YUN_MI_FA_JIANG_LIN；类: HeiYunMiFaJiangLin；牌池: 主牌池 |  |
| 如影随形 | 1 | 罕见 | 能力 | 回合开始时，若你处于黑云姿态，则抽1张牌，优先黑云牌。 | 黑云 | ID: YUKIMOD-RU_YING_SUI_XING；类: RuYingSuiXing；牌池: 无卡池；源码关键词: Innate |  |
| 引雷天云 | 1 | 罕见 | 能力 | 进入黑云姿态时，获得5/8层活力。 | 黑云 | ID: YUKIMOD-YIN_LEI_TIAN_YUN；类: YinLeiTianYun；牌池: 无卡池 |  |
| 黑雾降临 | 1 | 罕见 | 能力 | 回合开始时，进入黑云姿态。 | 黑云 | ID: YUKIMOD-HEI_WU_JIANG_LIN；类: HeiWuJiangLin；牌池: 主牌池；源码关键词: Innate |  |
| 剑舞 | 1 | 罕见 | 攻击 | 根据本回合额外抽牌的数量造成5/7点伤害（当前{CurrentHitCount}次）。 | 其他 | ID: YUKIMOD-JIAN_WU；类: JianWu；牌池: 主牌池 |  |
| 幻变斩 | 1 | 罕见 | 攻击 | 造成3点伤害2/3次。；消耗1张手牌。；抽1张牌。 | 其他 | ID: YUKIMOD-HUAN_BIAN_ZHAN；类: HuanBianZhan；牌池: 主牌池 |  |
| 看破 | 1 | 稀有 | 攻击 | 造成6/9点伤害。；将抽牌堆中的1张牌放入你的手牌。 | 其他 | ID: YUKIMOD-KAN_PO；类: KanPo；牌池: 主牌池 |  |
| 踏前斩 | 0 | 罕见 | 攻击 | 造成2/4点伤害。；对这回合没被踏前斩命中的敌人释放时返回手牌。 | 其他 | ID: YUKIMOD-TA_QIAN_ZHAN；类: TaQianZhan；牌池: 主牌池 |  |
| 零式 | 0 | 罕见 | 攻击 | 造成4/5点伤害。；将1张{IfUpgraded:show:一式+\|一式}加入手中。 | 其他 | ID: YUKIMOD-LING_SHI；类: LingShi；牌池: 主牌池；源码关键词: Exhaust |  |
| 映月 | 1 | 罕见 | 技能 | 下次打出月影时，将1张相同的复制加入手中。 | 其他 | ID: YUKIMOD-YING_YUE_MIRROR；类: YingYueMirror；牌池: 主牌池；源码关键词: Exhaust；升级移除消耗 |  |
| 暗月 | 1 | 罕见 | 技能 | 你手中的月影获得：；黑云：伤害增加50%。 | 其他 | ID: YUKIMOD-AN_YUE；类: AnYue；牌池: 无卡池；源码关键词: Exhaust；升级移除消耗 |  |
| 红尘 | 0 | 稀有 | 技能 | 获得1。；抽2/3张牌。；你在本回合内不能再抽任何牌。 | 其他 | ID: YUKIMOD-HONG_CHEN；类: HongChen；牌池: 主牌池 |  |
| 背水 | 0 | 罕见 | 技能 | 获得2/3。；你在本回合内不能再抽任何牌。 | 其他 | ID: YUKIMOD-BEI_SHUI；类: BeiShui；牌池: 主牌池 |  |
| 起手式 | 0 | 罕见 | 技能 | 获得1。 | 其他 | ID: YUKIMOD-QI_SHOU_SHI；类: QiShouShi；牌池: 主牌池；源码关键词: Retain, Exhaust；升级移除消耗 |  |
| 怒涛斩 | 1 | 罕见 | 能力 | 一回合内首次打出3张攻击牌时，获得1费。 | 其他 | ID: YUKIMOD-NU_TAO_ZHAN；类: NuTaoZhan；牌池: 主牌池；升级费用变化: -1 |  |
| 朔月 | 1 | 稀有 | 能力 | 回合结束时，凝聚1。 | 其他 | ID: YUKIMOD-SHUO_YUE；类: ShuoYue；牌池: 主牌池；源码关键词: Innate |  |
| 满月 | 1 | 罕见 | 能力 | 每当你使用1张牌，你手中的月影伤害+2/3。 | 其他 | ID: YUKIMOD-MAN_YUE；类: ManYue；牌池: 主牌池 |  |
| 盈月 | 1 | 罕见 | 能力 | 回合结束时，你手中的月影伤害增加5/7。 | 其他 | ID: YUKIMOD-YING_YUE；类: YingYue；牌池: 主牌池 |  |
| 蓝月 | 1 | 罕见 | 能力 | 当灵感触发时，你手中的月影伤害+3/5。 | 其他 | ID: YUKIMOD-LAN_YUE；类: LanYue；牌池: 主牌池 |  |
| 血月 | 1 | 罕见 | 能力 | 当你在黑云姿态下使用牌时，你手中的月影伤害+3/5。 | 其他 | ID: YUKIMOD-XUE_YUE；类: XueYue；牌池: 主牌池 |  |
| 拔刀斩 | 1 | 稀有 | 技能 | 抽1张牌并打出。；灵感：这张牌费用减少1。 | 灵感 | ID: YUKIMOD-BA_DAO_ZHAN；类: BaDaoZhan；牌池: 主牌池；源码关键词: Exhaust；升级移除消耗 |  |
| 雪影 | 1 | 稀有 | 技能 | 抽2张牌。；灵感：这张牌费用减少1。 | 灵感 | ID: YUKIMOD-XUE_YING；类: XueYing；牌池: 主牌池；源码关键词: Exhaust；升级移除消耗 |  |
| 黑云秘法：回转 | 1 | 罕见 | 技能 | 抽1张黑云牌。；灵感：获得2/3层黑云。；黑云：抽2张牌；否则，进入黑云姿态。 | 灵感 | ID: YUKIMOD-HEI_YUN_MI_JI_HUI_ZHUAN；类: HeiYunMiJiHuiZhuan；牌池: 无卡池 |  |
| 寒霜 | 1 | 稀有 | 能力 | 回合开始时，随机触发1/2张手中的灵感。 | 灵感 | ID: YUKIMOD-HAN_SHUANG；类: HanShuang；牌池: 主牌池 |  |
| 沉思 | 1 | 稀有 | 能力 | 当灵感触发时，获得2/3点格挡。 | 灵感 | ID: YUKIMOD-CHEN_SI；类: ChenSi；牌池: 主牌池 |  |
| 零度 | 1 | 稀有 | 能力 | 当你触发12/9次灵感时，将1张居合加入手中。 | 灵感 | ID: YUKIMOD-LING_DU；类: LingDu；牌池: 主牌池 |  |
| 霜降 | 1 | 稀有 | 能力 | 你的打击牌也可以触发灵感。；其灵感效果：费用减少1。 | 灵感 | ID: YUKIMOD-SHUANG_JIANG；类: ShuangJiang；牌池: 无卡池；源码关键词: Innate |  |
| 黑云奥义：黑雾 | 1 | 稀有 | 攻击 | 造成7/9点伤害。；黑云：你手中每有1张技能牌，额外攻击1次；否则，你手中每有1张攻击牌，额外攻击1次。 | 黑云 | ID: YUKIMOD-HEI_YUN_AO_YI_HEI_WU；类: HeiYunAoYiHeiWu；牌池: 主牌池 |  |
| 黑云心法 | 1 | 稀有 | 技能 | 本回合内保留黑云姿态。；黑云：本回合内获得2/3点力量。 | 黑云 | ID: YUKIMOD-HEI_YUN_XIN_FA；类: HeiYunXinFa；牌池: 主牌池 |  |
| 黑云秘法：出鞘 | 1 | 稀有 | 技能 | 进入黑云姿态。；下次退出黑云姿态时，将1张{IfUpgraded:show:纳刀+\|纳刀}加入手中。 | 黑云 | ID: YUKIMOD-HEI_YUN_MI_FA_CHU_QIAO；类: HeiYunMiFaChuQiao；牌池: 主牌池 |  |
| 黑云秘法：幕临 | 1 | 稀有 | 技能 | 黑云：获得1，抽1张牌，保留黑云姿态；否则，下次进入黑云姿态时额外获得2点能量，抽2张牌。 | 黑云 | ID: YUKIMOD-HEI_YUN_MI_FA_MU_LIN；类: HeiYunMiFaMuLin；牌池: 主牌池；源码关键词: Exhaust；升级费用变化: -1 |  |
| 黑云秘法：燕回 | 0 | 稀有 | 技能 | 将抽牌堆或弃牌堆中的随机1张黑云牌加入手中。；黑云：保留黑云姿态。 | 黑云 | ID: YUKIMOD-HEI_YUN_MI_FA_YAN_HUI；类: HeiYunMiFaYanHui；牌池: 主牌池；源码关键词: Exhaust；升级移除消耗 |  |
| 黑云秘法：霞阵 | 1 | 稀有 | 能力 | 回合开始时，获得2/3层黑云。 | 黑云 | ID: YUKIMOD-HEI_YUN_MI_FA_XIA_ZHEN；类: HeiYunMiFaXiaZhen；牌池: 主牌池 |  |
| 业火 | 1 | 稀有 | 攻击 | 造成5点伤害。；每当你攻击该目标时，手中月影伤害+2/3。 | 其他 | ID: YUKIMOD-YE_HUO；类: YeHuo；牌池: 主牌池 |  |
| 影月 | 0 | 稀有 | 攻击 | 造成5点伤害。；这张牌视为月影。；{IfUpgraded:show:每当你使用其他攻击命中时，这张牌伤害+1。\|} | 其他 | ID: YUKIMOD-SHADOW_MOON；类: ShadowMoon；牌池: 主牌池；源码关键词: Retain, Exhaust |  |
| 燕回反 | 1 | 稀有 | 攻击 | 释放你上一个释放的攻击1次。 | 其他 | ID: YUKIMOD-YAN_HUI_FAN；类: YanHuiFan；牌池: 主牌池；源码关键词: Exhaust；升级移除消耗 |  |
| 荣誉 | 1 | 稀有 | 攻击 | 造成9/12点伤害。；如果击杀目标，则将一张居合加入手中。 | 其他 | ID: YUKIMOD-RONG_YU；类: RongYu；牌池: 主牌池；源码关键词: Exhaust |  |
| 回天 | 1 | 稀有 | 技能 | 下一张攻击牌额外释放1次。 | 其他 | ID: YUKIMOD-HUI_TIAN；类: HuiTian；牌池: 主牌池；源码关键词: Exhaust；升级移除消耗 |  |
| 月读 | 0 | 稀有 | 技能 | 本回合内，你的攻击命中时，手中月影伤害+2/3。 | 其他 | ID: YUKIMOD-YUE_DU；类: YueDu；牌池: 主牌池 |  |
| 残月 | 1 | 稀有 | 技能 | 凝聚1。；你手中的月影费用减少1。 | 其他 | ID: YUKIMOD-CAN_YUE；类: CanYue；牌池: 主牌池；源码关键词: Exhaust；升级移除消耗 |  |
| 上弦之月 | 1 | 稀有 | 能力 | 当你打出的月影单次命中伤害达到50或更高时，将1张居合加入手中。 | 其他 | ID: YUKIMOD-SHANG_XIAN_ZHI_YUE；类: ShangXianZhiYue；牌池: 无卡池 |  |
| 天刀形态 | 3 | 稀有 | 能力 | 每回合打出的第一张攻击牌额外打出一次。 | 其他 | ID: YUKIMOD-TIAN_DAO_XING_TAI；类: TianDaoXingTai；牌池: 主牌池；源码关键词: Retain |  |
| 振刀 | 1 | 稀有 | 能力 | 每花费3/2费，抽1张牌。 | 其他 | ID: YUKIMOD-ZHEN_DAO；类: ZhenDao；牌池: 主牌池 |  |
| 满溢 | 1 | 稀有 | 能力 | 每回合第一次抽满手牌时，获得2费。 | 其他 | ID: YUKIMOD-MAN_YI；类: ManYi；牌池: 主牌池；升级费用变化: -1 |  |
| 瞬念 | 1 | 稀有 | 能力 | 回合开始时，将1张随机的{IfUpgraded:show:升级过的}灵感牌加入手中。 | 其他 | ID: YUKIMOD-SHUN_NIAN；类: ShunNian；牌池: 主牌池 |  |
| 黄昏的羁绊 | 1 | 稀有 | 能力 | 你不能额外获得费用。；回合开始时，随机2张手牌下次使用前费用减少1。 | 其他 | ID: YUKIMOD-HUANG_HUN_DE_JI_BAN；类: HuangHunDeJiBan；牌池: 主牌池；源码关键词: Innate；升级费用变化: -1 |  |
| 神丶压制准备 | 0 | 先古 | 技能 | 从抽牌堆或弃牌堆抽尽可能多的攻击牌。；获得2/5层活力。；灵感：优先抽灵感牌。 | 灵感 | ID: YUKIMOD-SHEN_YA_ZHI_ZHUN_BEI；类: ShenYaZhiZhunBei；牌池: 主牌池 |  |
| 天际斩击 | 1 | 先古 | 能力 | 回合开始时，将手中所有牌放到抽牌堆顶部，并抽出那个数量+2的牌。 | 其他 | ID: YUKIMOD-TIAN_JI_ZHAN_JI；类: TianJiZhanJi；牌池: 主牌池；源码关键词: Innate |  |
| 月影 | 0 | Token | 攻击 | 造成5点伤害。；其他攻击命中时，本牌伤害+1/2。 | 月影 | ID: YUKIMOD-YUE_YING；类: YueYing；牌池: Token牌池；源码关键词: Retain, Exhaust |  |
| 纳刀 | 1 | Token | 攻击 | 造成5/7点伤害。；本回合每释放一张攻击牌额外攻击一次。；手里没有其他攻击牌时费用-1。 | 黑云 | ID: YUKIMOD-NA_DAO；类: NaDao；牌池: Token牌池；源码关键词: Exhaust |  |
| 一式 | 1 | Token | 攻击 | 造成8/10点伤害。；将1张{IfUpgraded:show:二式+\|二式}加入手中。 | 其他 | ID: YUKIMOD-YI_SHI_TOKEN；类: YiShiToken；牌池: Token牌池；源码关键词: Exhaust |  |
| 三式 | 3 | Token | 攻击 | 造成32/40点伤害。；将1张{IfUpgraded:show:居合+\|居合}加入手中。 | 其他 | ID: YUKIMOD-SAN_SHI_TOKEN；类: SanShiToken；牌池: Token牌池；源码关键词: Exhaust |  |
| 二式 | 2 | Token | 攻击 | 造成16/20点伤害。；将1张{IfUpgraded:show:三式+\|三式}加入手中。 | 其他 | ID: YUKIMOD-ER_SHI_TOKEN；类: ErShiToken；牌池: Token牌池；源码关键词: Exhaust |  |
| 居合 | 0 | Token | 攻击 | 发动居合。 | 其他 | ID: YUKIMOD-JU_HE；类: JuHe；牌池: Token牌池；源码关键词: Exhaust |  |

## 统计

### 流派

| 流派 | 数量 |
| --- | ---: |
| 灵感 | 27 |
| 月影 | 4 |
| 黑云 | 22 |
| 其他 | 50 |

### 稀有度

| 稀有度 | 数量 |
| --- | ---: |
| 初始 | 4 |
| 通常 | 37 |
| 罕见 | 28 |
| 稀有 | 26 |
| 先古 | 2 |
| Token | 6 |

### 种类

| 种类 | 数量 |
| --- | ---: |
| 攻击 | 39 |
| 技能 | 41 |
| 能力 | 23 |
