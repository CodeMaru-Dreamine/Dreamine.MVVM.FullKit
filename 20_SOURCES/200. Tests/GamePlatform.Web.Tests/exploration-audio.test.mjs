import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';

const script = readFileSync(new URL('../../000.%20Project/010.%20App/GamePlatform.Web/wwwroot/maru-audio.js', import.meta.url), 'utf8');
const effect = { assetKey:'sword-hit', sourceUrl:'/hit.wav', maxVoices:8 };
function harness(pendingFetch) {
    const sources=[]; let fetches=0;
    const param=()=>({value:1,setTargetAtTime(){},setValueAtTime(){},linearRampToValueAtTime(){},exponentialRampToValueAtTime(){},cancelScheduledValues(){}});
    class Context {
        state='running';currentTime=10;destination={};sampleRate=24000;
        createGain(){return {gain:param(),connect(){return this;},disconnect(){}};}
        createBufferSource(){const node={playbackRate:{},started:false,stopped:false,connect(){return this;},disconnect(){},start(){this.started=true;},stop(){this.stopped=true;this.onended?.();}};sources.push(node);return node;}
        createOscillator(){return {frequency:param(),connect(){return this;},start(){},stop(){}};}
        resume(){return Promise.resolve();}
        decodeAudioData(){return Promise.resolve({duration:1});}
    }
    const window={AudioContext:Context};
    vm.runInNewContext(script,{window,console,performance:{now:()=>10000},fetch:()=>{fetches++;return pendingFetch ?? Promise.resolve({ok:true,arrayBuffer:async()=>new ArrayBuffer(0)});}});
    const api=window.maruAudio;api.initialize({},[]);
    return {api,sources,fetches:()=>fetches};
}
test('other menus block exploration effects before loading without blocking UI sounds',async()=>{
    const {api,sources,fetches}=harness();api.setExplorationMuted(true);
    assert.equal(await api.playEffect(effect,false),false);assert.equal(fetches(),0);assert.equal(sources.length,0);
    assert.equal(await api.playEffect('reward-claim',true),true);
    assert.equal(api.getDiagnostics().explorationMuted,true);
});
test('opening menu stops current exploration voices, not trial voices',async()=>{
    const {api,sources}=harness();
    assert.equal(await api.playEffect(effect,false),true);
    assert.equal(await api.playEffect(effect,false,'trial'),true);
    api.setExplorationMuted(true);
    assert.equal(sources[0].stopped,true);assert.equal(sources[1].stopped,false);
    assert.equal(api.getDiagnostics().activeEffects,1);
});
test('returning to exploration enables new effects',async()=>{
    const {api,sources}=harness();api.setExplorationMuted(true);api.setExplorationMuted(false);
    assert.equal(await api.playEffect(effect,false),true);assert.equal(sources[0].started,true);
});
test('late loaded effects cannot leak after opening and closing a menu',async()=>{
    let resolve;const pending=new Promise(r=>resolve=r);const {api,sources}=harness(pending);
    const sound=api.playEffect(effect,false);api.setExplorationMuted(true);api.setExplorationMuted(false);
    resolve({ok:true,arrayBuffer:async()=>new ArrayBuffer(0)});
    assert.equal(await sound,false);assert.equal(sources.length,0);
    assert.equal(await api.playEffect(effect,false),true);
});
