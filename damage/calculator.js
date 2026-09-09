/* Single calculation implementation shared by the browser and regression tests. */
(function(root,factory){const api=factory(typeof module==='object'&&module.exports?require('./calculator-levels.js'):root.damageLevels);if(typeof module==='object'&&module.exports)module.exports=api;else root.damageCalculator=api;})(typeof window==='object'?window:globalThis,function(levels){
'use strict';
const clamp=(v,a,b)=>Math.min(b,Math.max(a,v));
const n=(id,label,value,min=0,max=10000000,step='any')=>({id,label,value,min,max,step});
const pct=(id,label,value,min=0,max=100000)=>n(id,label+'（%）',value,min,max);
const sel=(id,label,value,options)=>({id,label,value,options});
const group=(title,fields)=>({title,fields});
const scaling=()=>[n('stat','结算属性值（攻击 / 生命 / 防御）',2000),pct('rate','本段技能倍率',200),n('flat','额外加算基础伤害',0)];
const critical=()=>[pct('cr','暴击率',80,0,1000),pct('cd','暴击伤害',160)];
const resistance=()=>[pct('res','敌人对应抗性',10,-100,1000),pct('resDown','减抗与抗性穿透合计',0)];
const defense=gi=>[n('level','角色等级',gi?90:80,1,gi?100:80,1),n('enemy','敌人等级',gi?100:95,1,999,1),pct('defDown','减防',0,0,gi?90:100),pct('ignore','无视防御',0,0,100)];
const ordinary=()=>[pct('bonus','适用增伤合计',50),...critical()];
const reaction=()=>[n('em','元素精通',200),pct('reactionBonus','反应伤害加成',0)];
const elementOptions=[['Physical','物理 / 裂伤'],['Fire','火 / 灼烧'],['Ice','冰 / 冻结'],['Lightning','雷 / 触电'],['Wind','风 / 风化'],['Quantum','量子 / 纠缠'],['Imaginary','虚数 / 禁锢']];
const modes={};
function add(game,id,title,groups,note){modes[game+'.'+id]={game,id,title,groups,note};}
const giEnemy=()=>group('敌人参数',[...defense(true),...resistance()]);
const giSpecial=()=>group('特殊机制：仅复现历史实测',[sel('targetReduction','特殊减伤状态','0',[['0','关闭：不使用此特殊机制'],['80','2.8 版海乱鬼起手动作实测（扣 80%）']])]);
const giBody=()=>group('角色与技能',[...scaling(),...ordinary()]);
add('gi','direct','普通直伤',[giBody(),giEnemy()],'单段伤害；结算属性填战斗中的最终值。多属性缩放可把其余“属性 × 倍率”之和填入加算基础伤害。');
add('gi','amplify','蒸发 / 融化',[giBody(),group('反应',[sel('reactionK','反应方向','1.5',[['1.5','反向：火蒸发 / 冰融化（1.5）'],['2','正向：水蒸发 / 火融化（2）']]),...reaction()]),giEnemy()],'只计算成功触发反应的这一段，不代表全技能反应覆盖率。');
add('gi','additive','超激化 / 蔓激化',[giBody(),group('激化',[sel('reactionK','激化类型','1.15',[['1.15','超激化（1.15）'],['1.25','蔓激化（1.25）']]),...reaction()]),giEnemy()],'已建立原激化状态；等级系数现支持 1–90 级，不对 91 级以上插值。');
add('gi','transform','剧变反应',[group('反应参数',[sel('reactionK','反应类型','3',[['3','超绽放 / 烈绽放 / 碎冰（3）'],['2.75','超载（2.75）'],['2','绽放 / 感电（2）'],['1.5','超导（1.5）'],['0.6','扩散（0.6）'],['0.25','燃烧（0.25）']]),n('level','触发者等级',90,1,90,1),...reaction()]),group('敌人参数',resistance())],'计算一次反应伤害，常规不暴击。抗性要选伤害实际元素；如超绽放查草抗，碎冰查物抗。');
add('gi','lunar','直接月反应',[group('基础与暴击',[...scaling(),sel('reactionK','月反应类型','3',[['3','直接月感电（3）'],['1','直接月绽放（1）'],['1.6','直接月结晶（1.6）']]),...critical()]),group('月反应加成',[...reaction(),pct('lunarBase','月反应基础加成',14),pct('elevation','提升（最终乘数 = 1 + 本值）',0)]),group('敌人参数',resistance())],'加算基础伤害视为 Q月，放在精通与反应增伤之后；不乘普通增伤和防御。已有数值对照仅覆盖伊涅芙月感电 Q=0、提升=0%，其他分支按研究公式演算。');
const hEnemy=(broken='1')=>group('敌人参数',[...defense(false),...resistance(),pct('vulnerability','适用易伤合计',0,0,250),pct('reduction','独立减伤的合并比例',0,0,100),sel('broken','结算时韧性状态',broken,[['1','已击破（×1）'],['0.9','未击破 / 首次击破（×0.9）']])]);
add('hsr','direct','普通直伤',[giBody(),hEnemy()],'单段技能伤害；易伤和抗性边界沿用页面注明的 Fribbels 口径。');
add('hsr','dot','技能持续伤害',[group('持续伤害参数',[...scaling(),pct('bonus','适用增伤合计',50)]),hEnemy()],'计算已成功施加的一跳 DoT，不再乘施加概率。常规不暴击；击破 DoT 请选“击破后续伤害”。');
const breakFields=()=>[pct('be','击破特攻',200),pct('breakBonus','本次适用的击破专属伤害加成',0)];
add('hsr','break','弱点击破',[group('击破参数',[sel('element','击破属性','Fire',elementOptions),n('toughness','敌人最大韧性（展示单位）',120),...breakFields()]),hEnemy('0.9')],'初次打空韧性的击破默认 ×0.9；120 展示韧性等于旧单位 360。普通增伤和双暴不参与。');
add('hsr','breakdot','击破后续伤害',[group('后续参数',[sel('element','后续效果','Physical',elementOptions),n('toughness','敌人最大韧性（展示单位）',120),n('hp','敌人生命上限',100000),sel('enemyType','敌人类型','boss',[['boss','精英 / 首领'],['normal','普通敌人']]),n('stacks','风化 / 纠缠层数',1,1,5,1),...breakFields()]),hEnemy()],'计算一跳。层数仅影响风化和纠缠，请填本次实际层数（精英风击破初始通常 3 层）；其他效果忽略层数。');
add('hsr','super','超击破',[group('超击破参数',[n('toughness','基础可受效率影响的削韧（展示单位）',40),pct('efficiency','击破效率加成',50),n('fixedToughness','不受效率影响的固定削韧',0),pct('conversion','超击破转化比例',100),...breakFields()]),hEnemy()],'基础削韧尚未计入效率。若已知最终削韧，效率填 0%。默认满足超击破触发条件，不自动判断角色赋能。');
add('hsr','elation','欢愉伤害',[group('欢愉参数',[pct('rate','欢愉技能倍率',200),pct('elation','欢愉度',60),pct('merry','增笑',20),n('punchline','本次读取的笑点 / 好活',20),...critical()]),hEnemy()],'仅支持 80 级欢愉基数 7535.107；普通增伤不参与。笑点与好活填技能本次实际读取的一项。');
add('hsr','true','真实伤害',[group('真实伤害参数',[n('original','符合条件的已结算原伤害',10000),pct('trueRatio','真实伤害比例',30)])],'原伤害已结算防御、抗性与易伤，这里不再重复乘；原伤害中的暴击结果保持原样。');
for(const key of ['gi.additive'])modes[key].groups.flatMap(g=>g.fields).find(f=>f.id==='level').max=90;
for(const key of ['gi.direct','gi.amplify','gi.additive'])modes[key].groups.push(giSpecial());
for(const key of ['gi.direct','gi.amplify','gi.additive'])modes[key].groups[0].fields.splice(2,0,pct('baseMultiplier','基础倍率修正（默认 100%）',100));
for(const mode of Object.values(modes))if(mode.game==='gi'){const f=mode.groups.flatMap(g=>g.fields).find(f=>f.id==='cr');if(f)f.min=-1000;}
for(const key of ['hsr.direct','hsr.dot'])modes[key].groups[0].fields.find(f=>f.id==='stat').value=3000;
modes['hsr.elation'].groups.flatMap(g=>g.fields).find(f=>f.id==='level').min=80;
function defaults(key){return Object.fromEntries(modes[key].groups.flatMap(g=>g.fields).map(f=>[f.id,f.value]));}
function calculate(key,raw){
 const mode=modes[key];if(!mode)throw Error('请选择已支持的伤害类型。');
 const v={};
 for(const f of mode.groups.flatMap(g=>g.fields)){
  const x=raw[f.id];
  if(f.options){if(!f.options.some(o=>o[0]===String(x)))throw Error('请选择'+f.label+'。');v[f.id]=String(x);}
  else{if(x==null||String(x).trim()===''||!Number.isFinite(Number(x)))throw Error('请填写'+f.label+'。');const y=Number(x);if(y<f.min||y>f.max||(f.step===1&&!Number.isInteger(y)))throw Error(f.label+'须为 '+f.min+'–'+f.max+(f.step===1?' 的整数':'')+'。');v[f.id]=y;}
 }
 const p=id=>(v[id]||0)/100,gi=mode.game==='gi',id=mode.id,terms=[];
 const term=(name,value)=>{terms.push({name,value});return value;};
 let base=0,mult=1,canCrit=false;
 if(id==='true'){const amount=v.original*p('trueRatio');return {normal:amount,crit:null,average:amount,total:v.original+amount,terms:[{name:'已结算原伤害',value:v.original},{name:'真伤比例',value:p('trueRatio')}],canCrit:false};}
 if(['direct','amplify','additive','dot','lunar'].includes(id))base=v.stat*p('rate')+v.flat;
 if(gi){
  if(['direct','amplify','additive'].includes(id))base=v.stat*p('rate')*p('baseMultiplier')+v.flat;
  if(id==='additive')base+=Number(v.reactionK)*levels.gi[v.level-1]*(1+5*v.em/(1200+v.em)+p('reactionBonus'));
  if(id==='transform')base=Number(v.reactionK)*levels.gi[v.level-1]*(1+16*v.em/(2000+v.em)+p('reactionBonus'));
  if(id==='lunar')base=Number(v.reactionK)*v.stat*p('rate')*(1+p('lunarBase'))*(1+6*v.em/(2000+v.em)+p('reactionBonus'))+v.flat;
  term('基础伤害（含适用反应项）',base);
  if(!['transform','lunar'].includes(id)){
   mult*=term(p('targetReduction')?'增伤与特殊减伤合并项':'增伤乘区',1+p('bonus')-p('targetReduction'));
   mult*=term('防御乘区',(v.level+100)/(v.level+100+(v.enemy+100)*(1-p('defDown'))*(1-p('ignore'))));
  }
  const r=p('res')-p('resDown');mult*=term('抗性乘区',r<0?1-r/2:r<.75?1-r:1/(4*r+1));
  if(id==='amplify')mult*=term('增幅反应',Number(v.reactionK)*(1+2.78*v.em/(1400+v.em)+p('reactionBonus')));
  if(id==='lunar')mult*=term('提升乘区',1+p('elevation'));
  canCrit=id!=='transform';
 }else{
  const lb=levels.hsr[v.level-1];
  if(['break','breakdot','super'].includes(id)){
   const t=.5+v.toughness/40,k={Physical:2,Fire:2,Ice:1,Lightning:1,Wind:1.5,Quantum:.5,Imaginary:.5};
   if(id==='break')base=lb*k[v.element]*t;
   if(id==='super')base=lb*(v.toughness*(1+p('efficiency'))+v.fixedToughness)/10*p('conversion');
   if(id==='breakdot')base={Physical:Math.min((v.enemyType==='normal'?.16:.07)*v.hp,2*lb*t),Fire:lb,Ice:lb,Lightning:2*lb,Wind:lb*v.stacks,Quantum:.6*lb*t*v.stacks,Imaginary:0}[v.element];
   term('基础伤害',base);mult*=term('击破特攻',1+p('be'))*term('击破专属加成',1+p('breakBonus'));
  }else if(id==='elation'){
   base=7535.107*p('rate');term('等级基数 × 技能倍率',base);mult*=term('欢愉度',1+p('elation'))*term('增笑',1+p('merry'))*term('笑点 / 好活',1+5*v.punchline/(v.punchline+240));
  }else{term('基础伤害',base);mult*=term('增伤乘区',1+p('bonus'));}
  mult*=term('防御乘区',(v.level+20)/(v.level+20+(v.enemy+20)*Math.max(0,1-p('defDown')-p('ignore'))));
  mult*=term('抗性乘区',1-clamp(p('res')-p('resDown'),-1,.9));
  mult*=term('易伤乘区',1+p('vulnerability'))*term('独立减伤',1-p('reduction'))*term('韧性乘区',Number(v.broken));
  canCrit=['direct','elation'].includes(id);
 }
 const critChance=clamp(p('cr'),0,1),normal=base*mult,crit=canCrit?normal*(1+p('cd')):null,average=canCrit?normal*(1+critChance*p('cd')):normal;
 if(![normal,average,crit??0].every(Number.isFinite))throw Error('结果超出可计算范围，请检查输入。');
 if(canCrit)terms.push({name:'期望暴击乘区',value:1+critChance*p('cd')});
 return {normal,crit,average,canCrit,terms,effectiveCritRate:critChance*100};
}
return {modes,defaults,calculate};
});
