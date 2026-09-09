(function(){
'use strict';
const api=window.damageCalculator,cache={},container=document.getElementById('calculator');let active=null,showHints=false;
const fmt=n=>n.toLocaleString('zh-CN',{minimumFractionDigits:2,maximumFractionDigits:2});
const precise=n=>Number(n.toPrecision(10)).toLocaleString('zh-CN',{maximumFractionDigits:8});
const friendlyFields={
 baseMultiplier:['基础倍率修正（%，默认 100）','普通技能填 100，表示乘 1。仅将明确修正基础倍率的效果填在这里，例如行秋四命生效时填 150；若已把该修正乘进技能倍率，保持 100，避免重复。此项不放大额外加算基础伤害。'],
 stat:['技能引用的属性数值','先读技能说明：写“攻击力的……”就填角色属性页的攻击力；写“生命上限的……”就填生命上限。使用本次攻击发生时的数值。'],
 rate:['技能倍率（%）','技能说明写“攻击力的 200%”，这里填 200。只取本次命中的倍率，不要把多次命中重复计入。'],
 flat:['额外增加的基础伤害','仅填明确额外加入计算起点的伤害数值。不是伤害百分比，也不是已打出的伤害；没有相应效果填 0。'],
 bonus:['本次适用的伤害加成合计（%）','“增伤”就是伤害加成。例如本次同时适用元素伤害 +30%、战技伤害 +20%，填 50。攻击力 +20% 不填这里。'],
 cr:['暴击率（%）','暴击是有概率发生的伤害放大。角色属性详情写 80%，填 80，表示每次有 80% 概率暴击。计算时按 0–100% 限定有效概率，超过 100% 不会继续提高期望伤害。'],
 cd:['暴击伤害（%）','角色属性详情写 160%，填 160。表示暴击时额外增加 160%，最终乘 2.6；不是乘 1.6。'],
 level:['角色等级','在角色资料中查看。涉及元素反应时，填写引起本次反应的角色等级。'],
 enemy:['敌人等级','看敌人信息或对应关卡资料。演算可用默认值；计算实际伤害时要换成实际目标等级。'],
 em:['元素精通','角色属性页中的“元素精通”，用于影响反应伤害。它是数值，不是百分比，例如 200 就填 200。'],
 reactionK:['反应类型','按这一次实际发生的反应选择。括号里的系数由计算器自动使用，不需要填入其他格子。'],
 reactionBonus:['所选反应的专用增伤（%）','只填明确提高这种反应伤害的效果。普通元素伤害加成不填这里；没有相应效果填 0。'],
 lunarBase:['月反应基础加成（%）','查对应天赋的“月反应基础伤害提升”效果，例如样例中的 14%。与反应伤害加成分开填写。'],
 elevation:['专属提升（%）','仅填写研究公式中单独的“提升”效果，不包括其他伤害加成。没有相应效果填 0；具体归类见进阶资料。'],
 res:['敌人对该伤害的抗性（%）','抗性表示敌人对某种属性伤害的抵抗。需查敌人资料；例如超绽放查草元素抗性。默认 10% 是示例条件，不适用于所有敌人。'],
 resDown:['降低抗性与抗性穿透合计（%）','只计入本次属性适用的减抗、抗性穿透效果，例如 20% 填 20；没有填 0。它与降低防御是两回事。'],
 defDown:['降低敌人防御（%）','防御会降低受到的伤害。只填技能等效果明确降低的防御百分比，不是敌人的防御数值；没有填 0。'],
 ignore:['本次无视防御（%）','只填技能等效果明确写出的“无视防御”比例；没有填 0。与降低防御分开，计算器会处理合并规则。'],
 vulnerability:['敌人受到伤害提高（%）','也称“易伤”。填写作用在敌人身上、适用于本次伤害的“受到伤害提高”效果合计；没有填 0。'],
 reduction:['敌人独立减伤的合并比例（%）','没有此效果填 0。多个独立减伤要按剩余比例相乘：20% 与 30% 合并为 1 − 0.8 × 0.7 = 44%，填 44。'],
 broken:['伤害结算时的韧性状态','韧性是血量之外的另一条数值，减少到零叫击破。按本次伤害发生时的状态选择；首次打空时产生的击破伤害选第二项。'],
 element:['击破的属性 / 对应效果','选择哪一种属性的攻击打空了敌人韧性。斜杠后是击破留下的效果名称。'],
 toughness:['敌人最大韧性（展示单位）','填敌人完整韧性条的总量，不是剩余量、血量或伤害。需查敌人资料；若资料使用旧削韧单位，先除以 3。'],
 hp:['敌人生命上限','填写敌人在本关卡的最大血量，不是当前剩余血量，也不是角色血量。可从对应敌人资料取得。'],
 enemyType:['敌人类型','根据目标是普通敌人，还是精英 / 首领选择。这里用于决定物理击破后裂伤的计算规则。'],
 stacks:['本次风化 / 纠缠层数','看敌人当前效果叠加了几层，填 1–5；只有风化和纠缠使用，选择其他效果时此数不影响结果。'],
 be:['击破特攻（%）','角色属性页写 200%，填 200。这项属性提高击破相关伤害，计算时对应乘 3。'],
 breakBonus:['本次适用的击破专用伤害加成（%）','只填明确适用于这一类击破伤害的专门加成；普通元素伤害加成不填这里。没有填 0。'],
 efficiency:['削减韧性的效率加成（%）','查技能或增益说明。例如基础削韧 40、效率 +50%，最终可削韧 60。与提高伤害的“击破特攻”不同。'],
 fixedToughness:['不受效率影响的额外削韧量','只有效果明确提供不受效率影响的固定削韧时才填写；没有填 0。使用与基础削韧相同的展示单位。'],
 conversion:['超击破转化比例（%）','从提供超击破的效果说明取得。写 100% 填 100，写 160% 填 160；不是普通技能伤害倍率。'],
 elation:['欢愉度（%）','查角色属性或当前相关效果中的“欢愉度”。60% 填 60；这项对应乘 1.6。'],
 merry:['增笑（%）','查对应技能或效果中的“增笑”。20% 填 20，对应再乘 1.2；没有此加成填 0。'],
 punchline:['技能本次读取的笑点 / 好活','按技能说明和当前战斗状态，填本次使用的那一种资源数值；不是百分比，也不是两者相加。'],
 original:['符合条件的已结算原伤害','填已经计入防御、抗性等因素的伤害结果，不是攻击力。只统计该真实伤害效果允许记录的部分。'],
 trueRatio:['追加的真实伤害比例（%）','查提供真实伤害的效果说明。例如按原伤害的 30% 追加，填 30。'],
 targetReduction:['历史特殊减伤状态','普通练习保持关闭。只有复现下面所列的特定历史实测时，才选择该状态。']
};
function fieldInfo(f,key){
 const info=[...(friendlyFields[f.id]||[f.label,''])];
 if(f.id==='reactionK')info[0]=f.label;
 if(f.id==='toughness'&&key==='hsr.super')return ['本次攻击的基础削韧量（展示单位）','查该技能本次攻击的削韧数据，不是技能伤害倍率。此处填尚未加效率的量；若填最终削韧量，效率必须填 0%。旧单位数据先除以 3。'];
 if(f.id==='flat'&&key==='gi.lunar')info[1]='仅填月反应专属的额外固定基础伤害，计算器会在元素精通和反应增伤之后加入。没有对应效果填 0。';
 if(f.id==='rate'&&key==='hsr.elation')info[0]='欢愉技能倍率（%）',info[1]='查本次欢愉技能伤害的倍率，写 200% 就填 200。这里使用等级基数，不填写攻击力。';
 if(f.id==='res'&&key.startsWith('hsr.'))info[1]='需查敌人对本次伤害属性的抗性；降低抗性、穿透另填下一项。默认 10% 只是示例条件，不是所有敌人的固定值。';
 return info;
}
function stepList(terms,raw,effectiveCritRate){let running=1;return terms.map((t,i)=>{const before=running;running=i===0?t.value:running*t.value;let name=t.name.replace('期望暴击乘区','按暴击概率取平均').replace('乘区','的影响'),expression=i===0?'从 '+fmt(running)+' 开始':fmt(before)+' × '+precise(t.value)+' → '+fmt(running),explanation='';if(i===0&&['gi.direct','gi.amplify','hsr.direct','hsr.dot'].includes(active)){name='先算技能的基础伤害';expression=`${fmt(Number(raw.stat))} × (${raw.rate} ÷ 100)${raw.baseMultiplier!==undefined&&Number(raw.baseMultiplier)!==100?' × ('+raw.baseMultiplier+' ÷ 100)':''}${Number(raw.flat)?' + '+fmt(Number(raw.flat)):''} → ${fmt(running)}`;}if(t.name==='期望暴击乘区')explanation=`平均倍数 = 1 + (${effectiveCritRate} ÷ 100) × (${raw.cd} ÷ 100) = ${precise(t.value)}`;if(t.name==='期望暴击乘区'&&Number(raw.cr)!==effectiveCritRate)explanation+=`（输入 ${raw.cr}%，有效暴击率按 ${effectiveCritRate}%）`;return `<li><span>${name}</span><strong>${expression}</strong>${explanation?`<small>${explanation}</small>`:''}</li>`;}).join('');}
const presets={
 'gi.direct':{label:'技能直接伤害（演算样例）',values:{}},
 'gi.amplify':{label:'200 精通反向蒸发',values:{em:200,reactionK:'1.5'}},
 'gi.additive':{label:'菲谢尔超激化（外部样本）',fixture:'fischl'},
 'gi.transform':{label:'1000 精通超绽放',values:{level:90,em:1000,reactionK:'3',res:10,reactionBonus:0}},
 'gi.lunar':{label:'伊涅芙协同攻击（外部样本）',values:{stat:2018.593074932,rate:65,flat:0,em:55,lunarBase:14,reactionBonus:0,elevation:0,cr:29.2,cd:155.128,res:10,resDown:0,reactionK:'3'}},
 'hsr.direct':{label:'80 级直伤演算',values:{stat:3000,rate:200,bonus:80,cr:70,cd:150,res:10}},
 'hsr.dot':{label:'已施加持续伤害（演算样例）',values:{stat:3000,rate:200,bonus:80,res:10}},
 'hsr.break':{label:'Guoba 火击破（外部样本）',values:{element:'Fire',level:80,enemy:95,toughness:120,be:150,defDown:40,res:20,resDown:30,vulnerability:20,broken:'0.9'}},
 'hsr.breakdot':{label:'Guoba 裂伤（外部样本）',values:{element:'Physical',level:80,enemy:95,toughness:120,hp:100000,enemyType:'boss',be:150,defDown:40,res:20,resDown:30,vulnerability:20,broken:'1'}},
 'hsr.super':{label:'Guoba 雪衣超击破（外部样本）',values:{level:80,enemy:95,toughness:40,efficiency:50,fixedToughness:0,be:200,conversion:160,res:0,resDown:25,ignore:20}},
 'hsr.elation':{label:'20 笑点（演算样例）',values:{cr:70,cd:150,res:10}},
 'hsr.true':{label:'原伤害 10000、真伤 30%',values:{original:10000,trueRatio:30}}
};
// Every source-specific preset is explicit; no character database or hidden build assumptions.
presets['gi.additive']={label:'200 精通超激化演算',values:{em:200,reactionK:'1.15'}};
function selectedValues(){return Object.fromEntries([...container.querySelectorAll('[data-field]')].map(e=>[e.dataset.field,e.value]));}
function results(){
 const box=container.querySelector('#calc-result');if(!active)return;
 const raw=selectedValues();cache[active]=raw;
 try{
  const r=api.calculate(active,raw);container.querySelector('#calc-error').textContent='';
  box.innerHTML=`<p class="eyebrow">${r.canCrit?'这一击的结果':'本次结算'}</p><div class="result-main"><span>${active==='hsr.true'?'额外真实伤害':r.canCrit?'期望伤害（长期平均）':'单次伤害'}</span><output>${fmt(r.average)}</output></div>${r.canCrit?`<div class="result-pair"><div><span>未暴击伤害</span><strong>${fmt(r.normal)}</strong></div><div><span>暴击伤害</span><strong>${fmt(r.crit)}</strong></div></div><p class="result-help">游戏中一次显示的伤害看上面两项。平均值表示相同条件下重复攻击的长期平均，不一定是某一次实际打出的数。</p>`:active==='hsr.true'?`<p class="result-help">原伤害 + 真伤合计</p><strong>${fmt(r.total)}</strong>`:'<p class="result-help">这一类伤害通常不使用普通暴击率和暴击伤害。</p>'}<details class="calc-steps"><summary>展开计算过程</summary><ol class="calc-step-list">${stepList(r.terms,raw,r.effectiveCritRate)}</ol><p>例如乘 1.5 就是提高 50%。过程数字为方便阅读做了舍入，实际计算保留完整精度。</p></details>`;
 }catch(error){box.innerHTML='<p class="result-empty">填写完整参数后显示结果</p>';container.querySelector('#calc-error').textContent=error.message;}
}
function fields(key){
 const mode=api.modes[key],values=cache[key]||api.defaults(key);active=key;
 const specialHelp=g=>g.fields.some(f=>f.id==='targetReduction')?'<p class="calc-note">正常配装计算保持关闭。此项只复现 KQM 记载的 2.8 版海乱鬼起手动作：从“1 + 增伤”里扣除 0.8，不能把它当作所有减伤的通用输入；也不是敌人的防御或抗性。本轮未做当前版本实机复测。<a href="https://library.keqingmains.com/evidence/combat-mechanics/damage/damage-formula#damage-reduction-mechanics" target="_blank" rel="noopener noreferrer">查看原始实测依据 ↗</a></p>':'';
 const input=f=>{const [label,hint]=fieldInfo(f,key);return `<label class="calc-field" for="calc-${f.id}"><span>${label}</span>${f.options?`<select id="calc-${f.id}" data-field="${f.id}" aria-describedby="hint-${f.id}">${f.options.map(([v,t])=>`<option value="${v}" ${String(values[f.id])===v?'selected':''}>${t}</option>`).join('')}</select>`:`<input id="calc-${f.id}" data-field="${f.id}" type="number" inputmode="decimal" value="${values[f.id]===''?'':Number(values[f.id])}" min="${f.min}" max="${f.max}" step="${f.step}" required ${hint?`aria-describedby="hint-${f.id}"`:''}>`}${hint?`<small id="hint-${f.id}" class="field-hint">${hint}</small>`:''}</label>`;};
 container.querySelector('#calc-content').innerHTML=`<p class="calc-note">输入参数后实时更新结果。百分比按显示值填写，例如 50% 填 50；参数含义可展开查看。</p><details class="data-help"><summary>参数来源与填写约定</summary><ol><li><strong>角色属性详情：</strong>找等级、攻击力、暴击率等数值。战斗中的临时提升也要计入。</li><li><strong>技能、装备和队友效果说明：</strong>找本次技能倍率及适用加成。每项效果只填一次，不要把“攻击力提高”当作“伤害提高”。</li><li><strong>对应关卡的敌人资料：</strong>找等级、抗性、最大韧性等。敌人资料常不在角色属性页中；不知道时使用假设值，只能得到假设条件下的结果。</li></ol><p>这个计算器需要手动填参数，不会自动读取角色装备或游戏画面。“没有该加成”可以填 0，“不知道数值”不能直接当成 0。</p></details><div class="calc-layout"><form id="calc-form" class="${showHints?'with-field-hints':'compact-fields'}" novalidate><div class="calc-actions"><button type="button" id="calc-preset">载入样例</button><button type="button" id="calc-reset">重置本类型</button><button type="button" id="calc-help-toggle" aria-pressed="${showHints}">${showHints?'收起参数说明':'展开参数说明'}</button><span id="calc-preset-name">${cache[key]?'保留了本类型的输入':'当前为默认参数'}</span></div>${mode.groups.map((g,i)=>`<details class="calc-group" ${i===0?'open':''}><summary>${g.title==='敌人参数'?'等级与敌人':g.title}<span>${i===0?'本次命中的结算参数':'展开调整'}</span></summary>${specialHelp(g)}<div class="calc-fields">${g.fields.map(input).join('')}</div></details>`).join('')}<details class="calc-group"><summary>这个计算器适用什么情况？</summary><p class="calc-note">${mode.note}</p></details><p id="calc-error" role="alert"></p></form><aside id="calc-result" aria-live="polite" aria-atomic="true"></aside></div>`;
 const form=container.querySelector('form');container.querySelector('#calc-help-toggle').onclick=e=>{showHints=!showHints;form.className=showHints?'with-field-hints':'compact-fields';e.currentTarget.setAttribute('aria-pressed',String(showHints));e.currentTarget.textContent=showHints?'收起参数说明':'展开参数说明';};form.addEventListener('submit',e=>e.preventDefault());const changed=()=>{container.querySelector('#calc-preset-name').textContent='已修改参数';results();};form.addEventListener('input',changed);form.addEventListener('change',changed);
 container.querySelector('#calc-reset').onclick=()=>{cache[key]=api.defaults(key);fields(key);container.querySelector('#calc-preset-name').textContent='已恢复默认参数';};
 container.querySelector('#calc-preset').onclick=()=>{cache[key]={...api.defaults(key),...presets[key].values};fields(key);container.querySelector('#calc-preset-name').textContent='已载入：'+presets[key].label;};
 results();
}
window.loadFirstDamageLesson=function(key){if(active!==key)return;cache[key]=api.defaults(key);fields(key);container.querySelector('#calc-preset-name').textContent='已填入算例参数（假设条件）';container.scrollIntoView({behavior:'smooth',block:'start'});};
window.mountDamageCalculator=function(game,topic){
 active=null;const available=Object.keys(api.modes).filter(k=>api.modes[k].game===game),key=game+'.'+topic,supported=available.includes(key);
 container.innerHTML=`<div class="calc-heading"><div><p class="eyebrow">参数计算</p><h3>${game==='gi'?'原神':'崩铁'}伤害计算器</h3></div><label class="calc-mode">计算类型<select id="calc-mode" aria-label="计算类型">${supported?'':'<option value="">选择伤害分支</option>'}${available.map(k=>`<option value="${k}" ${k===key?'selected':''}>${window.beginnerTopics?.[k]?.[0]||api.modes[k].title}</option>`).join('')}</select></label></div><div id="calc-content"></div>`;
 container.querySelector('#calc-mode').onchange=e=>{if(e.target.value){const id=api.modes[e.target.value].id,button=[...document.querySelectorAll('#types button')].find(b=>b.dataset.id===id);if(button)button.click();else fields(e.target.value);}};
 if(supported)fields(key);else container.querySelector('#calc-content').innerHTML='<p class="calc-note">这一主题还没有对应的数值计算器。上方的中文例子用于理解规则；需要计算其他伤害时，可在这里选择已支持的类型。</p>';
};
})();
