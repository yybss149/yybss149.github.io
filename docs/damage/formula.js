(function(){
'use strict';
const data=window.damageAtlas;let game='gi';
const auditScope={
 'gi.lunar':['部分参数已对照','外部数字仅覆盖直接月感电，且专属加算为 0、提升为 0%。月绽放、月结晶及非零专属项目前核对到研究公式，尚缺独立数值证据。'],
 'gi.lunar-team':['部分规则已对照','个人贡献和固定排序样例已核对。暴击可能改变排名，不能先取个人平均再排序；动态排名后的全队期望尚未验证。'],
 'gi.stellar':['研究公式','已查阅原作者正文与公式图，尚未完成独立数值对照；此主题不提供数值计算器。'],
 'hsr.elation':['引擎一致性已验证','80 级公式已与外部 CPU 函数核对；这不等于游戏实测，其他等级及角色特殊改写尚未验证。'],
 'hsr.true':['结算关系已核对','按符合条件的已结算原伤害追加比例。哪些伤害可记录、何时触发，仍需遵循具体技能说明。'],
 'gi.events':['规则说明','帮助拆分命中与反应；没有自动模拟队伍行动、元素附着或每个角色的触发条件。'],
 'hsr.events':['规则说明','帮助按事件汇总伤害；没有自动模拟行动轴、效果持续时间或每个角色的触发条件。']
};
const byId=id=>document.getElementById(id);
function show(item){
 window.mountDamageCalculator(game,item.id);
 byId('plain-guide').innerHTML=window.renderBeginnerContext(game,item.id)+(item.id==='direct'?window.renderFirstLesson(game):item.id==='lunar-team'?window.renderPlainGuide(game,item.id):'<details class="calculation-reading"><summary>计算逻辑与示例</summary>'+window.renderPlainGuide(game,item.id)+'</details>');
 byId('plain-guide').querySelectorAll('[data-first-lesson]').forEach(b=>b.onclick=()=>window.loadFirstDamageLesson(b.dataset.firstLesson));
 byId('advanced-reference').open=false;
 byId('description').hidden=true;
 byId('category').textContent=item.category;byId('title').textContent=window.beginnerTopics[game+'.'+item.id][0];byId('description').textContent=item.description;byId('status').textContent=item.status||'通用公式';
 const scope=auditScope[game+'.'+item.id]||['基础公式已对照','所列基础分支已通过外部结果与边界核对；仅计算本次输入已表达的效果。未单列的独立修正默认为 1，角色特殊机制需另行确认。'];
 byId('status').textContent=scope[0];byId('audit-scope').textContent=scope[1];
 byId('formula').innerHTML=item.factors.map((f,i)=>`${i?'<b>'+(i===1?'=':'×')+'</b>':''}<span>${f}</span>`).join('');
 byId('formula-note').textContent=item.note;
 byId('details').innerHTML=item.details.map((d,i)=>`<details><summary><span class="index">${String(i+1).padStart(2,'0')}</span><strong>${d[0]}</strong><small>${d[1]}</small></summary><div class="detail-body">${d[2]}</div></details>`).join('');
 byId('notes').innerHTML=item.notes.length?'<h3>计算时容易漏掉的细节</h3><ul>'+item.notes.map(n=>'<li>'+n+'</li>').join('')+'</ul>':'';
 byId('refs').innerHTML=item.refs.map(id=>{const s=data.sources.find(s=>s.id===id);return `<a href="${s.url}" target="_blank" rel="noopener noreferrer">[${s.id}] ${s.short} ↗</a>`}).join('');
 document.querySelectorAll('#types button').forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.id===item.id)));
}
function setGame(next){game=next;document.body.dataset.game=game;document.querySelectorAll('[data-game]').forEach(b=>{if(b.tagName==='BUTTON')b.setAttribute('aria-pressed',String(b.dataset.game===game))});const items=data.games[game];byId('types').innerHTML=items.map(x=>`<button type="button" data-id="${x.id}" aria-pressed="false">${window.beginnerTopics[game+'.'+x.id][0]}</button>`).join('');byId('types').querySelectorAll('button').forEach(b=>b.addEventListener('click',()=>show(items.find(x=>x.id===b.dataset.id))));if(items.length)show(items[0]);}
document.querySelectorAll('.games button').forEach(b=>b.addEventListener('click',()=>setGame(b.dataset.game)));
byId('source-list').innerHTML=data.sources.map(s=>`<div class="source"><a href="${s.url}" target="_blank" rel="noopener noreferrer">[${s.id}] ${s.title} ↗</a><span>${s.scope}</span></div>`).join('');
byId('coverage').innerHTML=data.coverage||'';setGame(game);
})();
