import {Injectable, Scope} from '@nestjs/common';
import {CreateRoleDto} from './dto/create-role.dto';
import {Prisma, Role} from "@prisma/client";
import { FilterQueryDto } from '@/modules/filter/dto/query-filter.dto';
import { BaseORMFilter } from '@/modules/filter/base/base-ORM-filter';
import { PrismaService } from 'prisma/prisma.service';
import { ResponseImpl } from '@/wrapper/imeplement/response.implement';

export type RoleQueryFilter = Omit<FilterQueryDto, | "firstName" | "lastName" | "email">;

@Injectable({ scope: Scope.REQUEST })
export class RoleService extends BaseORMFilter {
    constructor(private readonly prismaService: PrismaService) {
        super();
    }
    
    async create(request: CreateRoleDto) {
        const role: Role = await this
            .prismaService
            .role.create({
                data: request
            });
        return ResponseImpl.success(role);
    }

    async findAll(filter: RoleQueryFilter) {
        const where: Prisma.RoleWhereInput = this.filter(filter);
        const data: Role[] = await this
            .prismaService
            .role
            .findMany({
                where,
            });
        return ResponseImpl.success(data);
    }

    async findOne(id: string) {
        const data: Role[] = await this
            .prismaService
            .role
            .findMany({
                where: { id },
            });
        return ResponseImpl.success(data);
    }

    async update(id: string, request: CreateRoleDto) {
        const data: Role = await this
            .prismaService
            .role
            .update({
                where: { id },
                data: request
            });
        return ResponseImpl.success(data);
    }
}
