"""Independent read-back of synthetic smoke-test PSTs. Requires Python 3.12+,
and pypstreader==1.0.0. It never opens real user mail."""
import json
import sys
from pathlib import Path
import pypstreader

root = Path(sys.argv[1] if len(sys.argv) > 1 else "artifacts/smoke")
outputs = json.loads((root / "outputs.json").read_text(encoding="utf-8-sig"))
expected = (root / "expected-attachment.bin").read_bytes()
seen = set()
count = 0
for path in outputs:
    with pypstreader.open(path) as store:
        for folder in store.root_folder.walk():
            for message in folder.messages():
                eml = pypstreader.to_eml(message)
                subject = str(eml["Subject"])
                assert subject.startswith("Caffè e città — "), subject
                assert subject not in seen, subject
                seen.add(subject)
                assert "alice@example.org" in str(eml["From"])
                assert "bob@example.org" in str(eml["To"])
                assert "clara@example.org" in str(eml["Cc"])
                assert "davide@example.org" in str(eml["Bcc"])
                assert "smoke-" in str(eml["Message-ID"])
                html = eml.get_body(preferencelist=("html",))
                assert html is not None and "<b>Ciao, città e caffè.</b>" in html.get_content()
                attachments = list(eml.iter_attachments())
                assert len(attachments) == 1, len(attachments)
                assert attachments[0].get_filename() == "prova.bin"
                assert attachments[0].get_payload(decode=True) == expected
                count += 1
assert count == 240, count
print(json.dumps({"reader": "pypstreader 1.0.0", "messages": count,
                  "pst_parts": len(outputs), "attachments_byte_identical": count,
                  "unicode_html_recipients_message_ids": "passed"}, indent=2))
