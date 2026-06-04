import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ButtonsModule } from '@progress/kendo-angular-buttons';

export interface RowActionItem<TAction extends string = string> {
  text: string;
  action: TAction;
  disabled?: boolean;
}

@Component({
  selector: 'app-row-action-menu',
  standalone: true,
  imports: [CommonModule, ButtonsModule],
  templateUrl: './row-action-menu.component.html'
})
export class RowActionMenuComponent<TAction extends string = string> {
  @Input() actions: RowActionItem<TAction>[] = [];
  @Input() disabled = false;
  @Input() ariaLabel = 'More actions';

  @Output() actionSelected = new EventEmitter<RowActionItem<TAction>>();

  select(event: RowActionItem<TAction> | { item?: RowActionItem<TAction> }): void {
    const item = (event as { item?: RowActionItem<TAction> }).item ?? event as RowActionItem<TAction>;

    if (!item || item.disabled) {
      return;
    }

    this.actionSelected.emit(item);
  }
}
