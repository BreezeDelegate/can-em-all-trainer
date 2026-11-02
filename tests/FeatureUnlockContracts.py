from pathlib import Path
r=Path(__file__).resolve().parents[1]
a=(r/'src/NativeActions.cs').read_text()
p=(r/'src/NativePanel.cs').read_text()
l=(r/'src/Localization.cs').read_text()
checks={
"casino uses native email":'DeliverEmail(MailId.FeelingLucky,"Casino")' in a,
"factory uses native buyout email":'DeliverEmail(MailId.AnOfferYouCantRefuse' in a,
"email accepted through game":'mm.Receive(id)' in a and 'mm.HasAccepted(id)' in a and 'mm.HasReceived(id)' in a,
"email persisted":'sm.Save();' in a,
"current count preserved":'if(d.totalCans<100000L)d.totalCans=100000L;' in a,
"lifetime count preserved":'if(d.lifetimeStats.totalCans<100000L)' in a,
"native Steam progress":'SteamManager.UnlockProgress(100000)' in a,
"casino button even when slot exists":p.count('NativeActions.SendCasinoEmail')==2,
"factory button wired":p.count('NativeActions.SendFactoryOffer')==1,
"children button wired":p.count('NativeActions.SetChildrenFed100000')==1,
"bilingual buttons":all(x in l for x in ["SEND CASINO EMAIL","SEND FACTORY OFFER","SET 100,000 FED"]),
"no automatic trigger in runtime tick":all(x not in (r/'src/AdminPanel.cs').read_text() for x in ("SendCasinoEmail","SendFactoryOffer","SetChildrenFed100000"))
}
for name,ok in checks.items():
 print(("PASS " if ok else "FAIL ")+name)
assert all(checks.values())
print(f"FEATURE_UNLOCK_CONTRACTS={len(checks)}")
