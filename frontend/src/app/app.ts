import { Component, ElementRef, afterRenderEffect, inject, viewChild } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Notification } from './core/services/notification';

@Component({
  imports: [RouterOutlet],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly notification = inject(Notification);
  private readonly banner = viewChild<ElementRef<HTMLElement>>('notificationBanner');

  constructor() {
    afterRenderEffect(() => {
      const message = this.notification.message();
      const element = this.banner()?.nativeElement;
      if (message && element) {
        // Show the shared interceptor notification above native modal dialogs.
        if (element.matches(':popover-open')) element.hidePopover();
        element.showPopover();
      }
    });
  }
}

