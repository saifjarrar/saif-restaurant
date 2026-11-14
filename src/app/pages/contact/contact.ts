import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-contact',
  styleUrl: './contact.css',
  templateUrl: './contact.html',
})
export class Contact {
  private readonly http = inject(HttpClient);

  readonly sending = signal(false);
  readonly status = signal('');

  send(event: Event, form: HTMLFormElement): void {
    event.preventDefault();

    if (this.sending() || !form.reportValidity()) return;

    const data = new FormData(form);
    const value = (key: string) => String(data.get(key) ?? '').trim();
    const body = {
      name: value('name'),
      email: value('email'),
      phone: value('phone'),
      topic: value('topic'),
      message: value('message'),
    };

    if (!body.name || !body.message) {
      this.status.set('Enter your name and message.');
      return;
    }

    this.sending.set(true);
    this.status.set('');

    this.http
      .post<void>('https://localhost:7118/api/contact', body)
      .pipe(finalize(() => this.sending.set(false)))
      .subscribe({
        next: () => {
          form.reset();
          this.status.set('Message sent. Thank you!');
        },
        error: () => this.status.set('Could not send your message. Please try again.'),
      });
  }
}
